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

  async function loadDataSetByWeekAndYear(week: number, year: number) {
    loading.value = true;
    const req = new DataSetRequestDto();
    req.week = week;
    req.year = year;
    const subscriptionData: StreamSubscription<DataSetResponseDto> = {
      streamId: "datasets.get",
      next: (chunk: DataSetResponseDto[]) => {
        logInfo(`Received data set chunk: ${chunk.length} records`);
        chunk.forEach((dto: DataSetResponseDto) => {
          logInfo(
            `DataSet - ID: ${dto.id}, Week: ${dto.week}, Year: ${dto.year}, License: ${dto.license}, VIN: ${dto.vIN}, Model: ${dto.model}, AppName: ${dto.appName}, ResultType: ${dto.resultType}, ErrorCode: ${dto.errorCode}`,
          );
        });
      },
      completed: () => {
        logSuccess("Data set stream completed");
      },
      error: (err: any) => {
        logError("Data set stream error: " + err);
      },
    };

    try {
      bus.subscribeStream<DataSetResponseDto>(subscriptionData, req);

      // user.value = result
    } finally {
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
                `week: ${r.week} year: ${r.year} name: ${r.name} startImportedAt: ${r.startImportedAt.toISOString()} endImportedAt: ${r.endImportedAt.toISOString()}`,
            )
            .join("\n"),
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
    loadDataSetByWeekAndYear,
    loadImportedFiles,
    uploadFile,
    clear,
  };
}
