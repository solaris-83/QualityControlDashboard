import { ref, computed } from "vue";
import { UserDto } from "../models/user-dto";
import { bus } from "../services/webviewMessenger";
import { FileRequestDto } from "../models/file-request-dto";
import { logError, logSuccess, logInfo } from "../miscellanea/log";
import { ImportResultDto } from "../models/import-result-dto";
import { ImportProgressDto } from "../models/import-progress-dto";
import { FileResponseDto } from "../models/file-response-dto";
import { Constants } from "../models/constants";

export function useImportFiles() {
  const loading = ref(false);
  const uploading = ref(false);
  const deleting = ref(false);
  const dataSetInfo = ref<string | null>(null);
  const dataSetRows = ref<FileResponseDto[]>([]);
  const dataSetError = ref<string | null>(null);

  const errors = ref<string[]>([]);

  // Function to load imported files based on week, year, and projects
  // Sends a request to the backend (bus.request), waits for a response and updates the state with the retrieved file information
  async function loadImportedFiles(week: number, year: number, projects: string[],
  ) {
    dataSetInfo.value = "Starting to load imported files...";
    dataSetError.value = null;
    dataSetRows.value = [];
    loading.value = true;

    try {
      const fileRequest: FileRequestDto = new FileRequestDto();
      fileRequest.week = week;
      fileRequest.year = year;
      fileRequest.projects = projects;

      const results: FileResponseDto[] = await bus.request(
        Constants.files_Get,
        fileRequest,
      );
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

  // Function to delete imported files and datasets correlated rows 
  // Sends a request to the backend (bus.request), waits for a response and updates the state with the retrieved file information
  async function deleteImportedFiles(fileIds: number[]) {
    dataSetInfo.value = "Starting to delete imported files and correlated rows...";
    dataSetError.value = null;
    deleting.value = true;

    try {
      

      const results: FileResponseDto[] = await bus.request(
        Constants.files_Delete,
        fileIds,
      );
     // dataSetRows.value = [...dataSetRows.value, ...results];
      dataSetInfo.value = `Number of files deleted: ${results.length}`;
      logSuccess(dataSetInfo.value);
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : "Failed to delete imported files and correlated rows.";
      dataSetError.value = errorMessage;
      dataSetInfo.value = null;
      logError("Error deleting imported files: " + errorMessage);
    } finally {
      deleting.value = false;
    }
  }

  // Function to upload a file based on the provided file request information
  // Subscribes to upload progress updates (bus.subscribe<ImportProgressDto>) and sends an upload request to the backend (bus.request)
  async function uploadFile(fileRequest: FileRequestDto) {
    uploading.value = true;
    dataSetInfo.value = "Starting file upload...";
    dataSetError.value = null;

    try {
      bus.subscribe<ImportProgressDto>(
        Constants.files_Upload_Progress,
        (msg) => {
          dataSetInfo.value = `${msg.fileName} ${msg.percentComplete}% - ${msg.currentStatus}`;
          logInfo(dataSetInfo.value);
        },
      );

      const result: ImportResultDto[] = await bus.request<ImportResultDto[]>(
        Constants.files_Upload,
        fileRequest,
        Constants.importFileMaxTimeoutSeconds * 1000, // Set timeout for the upload request
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
    deleteImportedFiles
  };
}
