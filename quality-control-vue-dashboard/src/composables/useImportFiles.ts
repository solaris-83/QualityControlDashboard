import { ref, computed } from "vue";
import { UserDto } from "../models/user-dto";
import { bus } from "../services/webviewMessenger";
import { FileRequestDto } from "../models/file-request-dto";
import { logError, logSuccess, logInfo } from "../miscellanea/log";
import { ImportResult } from "../models/import-result";
import { ImportProgress } from "../models/import-progress";
import { DataSetResponseDto } from "../models/data-set-response-dto";
import { DataSetRequestDto } from "../models/data-set-request-dto";
import { StreamSubscription } from "../models/streamSubscription";
import { FileResponseDto } from "../models/file-response-dto";

export function useImportFiles() {
  const loading = ref(false);
  const uploading = ref(false);
  const dataSetInfo = ref<string | null>(null);
  const dataSetRows = ref<FileResponseDto[]>([]);
  const dataSetError = ref<string | null>(null);

  const errors = ref<string[]>([]);

  async function loadImportedFiles() {
    dataSetInfo.value = "Starting to load imported files...";
    dataSetError.value = null;
    dataSetRows.value = [];
    loading.value = true;

    try {
      const results: FileResponseDto[] = await bus.request("files.get", null);
      dataSetRows.value = [...dataSetRows.value, ...results];
      dataSetInfo.value = `Number of files retrieved: ${results.length}`;
      logSuccess(dataSetInfo.value);
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : "Failed to load imported files.";
      dataSetError.value = errorMessage;
      dataSetInfo.value = null;
      logError("Error loading imported files: " + errorMessage);
    } finally {
      loading.value = false;
    }
  }

  async function uploadFile(fileRequest: FileRequestDto) {
    uploading.value = true;
    dataSetInfo.value = "Starting file upload...";
    dataSetError.value = null;

    try {
      bus.subscribe<ImportProgress>("csv.upload.progress", (msg) => {
        dataSetInfo.value = `Upload progress: ${msg.percentComplete}% - ${msg.currentStatus}`;
        logInfo(dataSetInfo.value);
      });

      const result: ImportResult[] = await bus.request<ImportResult[]>(
        "files.upload",
        fileRequest,
        1800000,
      );

      dataSetInfo.value = `${result.length > 0 ? result[0].fullName : "No files"} imported.`;
      logSuccess(dataSetInfo.value);
      /* logSuccess(
        "Upload result:" +
          result
            .map(
              (r) =>
                `${r.fullName}: ${r.success ? "Success" : "Failed"} (${r.recordsImported} imported, ${r.recordsSkipped} skipped)`,
            )
            .join("\n"),
      );*/
    } catch (err) {
      dataSetInfo.value = null;
      dataSetError.value =
        err instanceof Error ? err.message : "Failed to load dataset.";
      loading.value = false;
    } finally {
      uploading.value = false;
    }
  }

  return {
    loading,
    uploading,
    errors,
    dataSetInfo,
    dataSetRows,
    dataSetError,
    loadImportedFiles,
    uploadFile,
  };
}
