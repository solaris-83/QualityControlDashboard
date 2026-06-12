import { ref } from "vue";
import { bus } from "../services/webviewMessenger";
import { DataSetResponseDto } from "../models/data-set-response-dto";
import { DataSetRequestDto } from "../models/data-set-request-dto";
import { StreamSubscription } from "../models/streamSubscription";
import { Constants } from "../models/constants";
import { logError, logInfo, logSuccess } from "../miscellanea/log";

export function useDataSet() {
  const loading = ref(false);
  const dataSetInfo = ref<string | null>(null);
  const dataSetRows = ref<DataSetResponseDto[]>([]);
  const dataSetError = ref<string | null>(null);

  async function loadDataSetByWeekAndYear(
    week: number,
    year: number,
    pageSize: number = 100,
    pageNumber: number = 1,
  ) {
    loading.value = true;
    dataSetInfo.value = "Starting to load data set...";
    dataSetError.value = null;
    dataSetRows.value = [];

    const req = new DataSetRequestDto();
    req.week = week;
    req.year = year;
    req.pageSize = pageSize;
    req.pageNumber = pageNumber;

    const subscriptionData: StreamSubscription<DataSetResponseDto> = {
      streamId: Constants.dataset_Get,
      next: (chunk: DataSetResponseDto[], chunkIndex: number) => {
        dataSetRows.value = [...dataSetRows.value, ...chunk];
        dataSetInfo.value = `Received data set chunk: ${chunk.length} records (Chunk Index: ${chunkIndex})`;
        logInfo(dataSetInfo.value)
       /* chunk.forEach((dto: DataSetResponseDto) => {
          logInfo(
            `DataSet - ID: ${dto.id}, Week: ${dto.week}, Year: ${dto.year}, License: ${dto.license}, VIN: ${dto.vin}, Model: ${dto.model}, AppName: ${dto.appName}, ElapsedTime: ${dto.elapsedTime}, AffectedControllers: ${dto.affectedControllers}, ResultType: ${dto.resultType}, ErrorCode: ${dto.errorCode}`,
          );
        });*/
      },
      completed: () => {
        dataSetError.value = null;
        dataSetInfo.value = `Data set loading completed. Total records received: ${dataSetRows.value.length}.`;
        logSuccess(dataSetInfo.value);
        loading.value = false;
      },
      error: (err: any) => {
        logError("Data set stream error: " + err);
        dataSetInfo.value = null;
        dataSetError.value = err instanceof Error ? err.message : String(err);
        loading.value = false;
      },
    };

    try {
      bus.subscribeStream<DataSetResponseDto>(subscriptionData, req);
    } catch (err) {
      dataSetInfo.value = null;
      dataSetError.value = err instanceof Error ? err.message : "Failed to load dataset.";
      loading.value = false;
    }
  }

  return {
    loading,
    dataSetInfo,
    dataSetRows,
    dataSetError,
    loadDataSetByWeekAndYear,
  };
}
