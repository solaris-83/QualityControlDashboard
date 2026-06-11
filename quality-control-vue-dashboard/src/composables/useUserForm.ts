import { ref, computed } from "vue";
import { UserDto } from "../models/user-dto";
import { bus } from "../services/webviewMessenger";
import { FileRequestDto } from "../models/file-request-dto";
import { logError, logSuccess, logInfo } from "../log";
import { ImportResult } from "../models/import-result";
import { ImportProgress } from "../models/import-progress";
import { DataSetResponseDto } from "../models/data-set-response-dto";
import { DataSetRequestDto } from "../models/data-set-request-dto";
import { StreamSubscription } from "../services/streamSubscription";
import { FileResponseDto } from "../models/file-response-dto";

export function useUserForm() {
  const loading = ref(false);
  const dataSetRows = ref<DataSetResponseDto[]>([]);
  const dataSetError = ref<string | null>(null);

  const saving = ref(false);

  const user = ref<UserDto>({
    id: 0,
    firstName: "",
    lastName: "",
    email: "",
    age: 18,
  });

  const errors = ref<string[]>([]);

  const isValid = computed(() => {
    errors.value = [];

    if (!user.value.firstName) errors.value.push("First Name required");

    if (!user.value.lastName) errors.value.push("Last Name required");

    if (!user.value.email) errors.value.push("Email required");

    return errors.value.length === 0;
  });

  async function loadDataSetByWeekAndYear(week: number, year: number, pageSize: number = 100, pageNumber: number = 1) {
    loading.value = true;
    dataSetError.value = null;
    dataSetRows.value = [];

    const req = new DataSetRequestDto();
    req.week = week;
    req.year = year;
    req.pageSize = pageSize;
    req.pageNumber = pageNumber;
    const subscriptionData: StreamSubscription<DataSetResponseDto> = {
      streamId: "datasets.get",
      next: (chunk: DataSetResponseDto[], chunkIndex: number) => {
        dataSetRows.value = [...dataSetRows.value, ...chunk];
        logSuccess(`Received data set chunk: ${chunk.length} records (Chunk Index: ${chunkIndex})`);
        chunk.forEach((dto: DataSetResponseDto) => {
          logInfo(
            `DataSet - ID: ${dto.id}, Week: ${dto.week}, Year: ${dto.year}, License: ${dto.license}, VIN: ${dto.vin}, Model: ${dto.model}, AppName: ${dto.appName}, ElapsedTime: ${dto.elapsedTime}, AffectedControllers: ${dto.affectedControllers}, ResultType: ${dto.resultType}, ErrorCode: ${dto.errorCode}`,
          );
        });
      },
      completed: () => {
        logSuccess("Data set stream completed");
        loading.value = false;
      },
      error: (err: any) => {
        logError("Data set stream error: " + err);
        dataSetError.value = err instanceof Error ? err.message : String(err);
        loading.value = false;
      },
    };

    try {
      bus.subscribeStream<DataSetResponseDto>(subscriptionData, req);
    } catch (err) {
      dataSetError.value = err instanceof Error ? err.message : "Failed to load dataset.";
      loading.value = false;
    }
  }

  async function loadImportedFiles() {

    saving.value = true;

    try {
      const results : FileResponseDto[] = await bus.request("files.get", null);
      logSuccess("Files retrieved: " + results
            .map(
              (r) =>
                `week: ${r.week} year: ${r.year} name: ${r.name} startImportedAt: ${r.startImportedAt.toLocaleString()} endImportedAt: ${r.endImportedAt.toLocaleString()}`,
            )
            .join("\n"))
    } 
    finally {
      saving.value = false;
    }
  }

  async function uploadFile(fileRequest: FileRequestDto) {
    loading.value = true;

    try {
      bus.subscribe<ImportProgress>("csv.upload.progress", (msg) => {
        logInfo(
          "Upload progress: " +
            msg.percentComplete.toString() +
            "% - " +
            msg.currentStatus,
        );
      });

      const result: ImportResult[] = await bus.request<ImportResult[]>(
        "files.upload",
        fileRequest,
        1800000,
      );

      logSuccess(
        "Upload result:" +
          result
            .map(
              (r) =>
                `${r.fullName}: ${r.success ? "Success" : "Failed"} (${r.recordsImported} imported, ${r.recordsSkipped} skipped)`,
            )
            .join("\n"),
      );
    } finally {
      loading.value = false;
    }
  }

  function clear() {
    user.value = {
      id: 0,
      firstName: "",
      lastName: "",
      email: "",
      age: 18,
    };
  }

  return {
    user,
    loading,
    saving,
    errors,
    isValid,
    dataSetRows,
    dataSetError,
    loadDataSetByWeekAndYear,
    loadImportedFiles,
    uploadFile,
    clear,
  };
}
