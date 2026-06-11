import { reactive, ref } from "vue";
import { FileResponseDto } from "../models/file-response-dto";
import { bus } from "../services/webviewMessenger";


export function useFilesTable() {
    const loadingTable = ref(false);
  const tableError = ref<string | null>(null);
     // Table config
    const table = reactive({
      isLoading: false,
      columns: [
        {
          label: "ID",
          field: "id",
          width: "3%",
          sortable: true,
          isKey: true,
        },
        {
          label: "Week",
          field: "week",
          width: "10%",
          sortable: true,
        },
        {
          label: "Year",
          field: "year",
          width: "10%",
          sortable: true,
        },
        {
          label: "Name",
          field: "name",
          width: "15%",
          sortable: true,
        },
        {
          label: "Start Import",
          field: "startImportedAt",
          width: "35%",
          sortable: true,
        },
        {
          label: "End Import",
          field: "endImportedAt",
          width: "35%",
          sortable: true,
        },
      ],
      rows: [] as FileResponseDto[],
      totalRecordCount: 0,
      sortable: {
        order: "id",
        sort: "asc",
      },
    });


    async function loadFiles() {
    tableError.value = null;
        loadingTable.value = true;
    table.isLoading = true;
        try {
      const results = await bus.request<FileResponseDto[]>("files.get", null);
      const safeResults = Array.isArray(results) ? results : [];

      table.rows = safeResults;
      table.totalRecordCount = safeResults.length;
    } catch (error) {
      table.rows = [];
      table.totalRecordCount = 0;
      tableError.value = error instanceof Error ? error.message : "Failed to load files.";
        } finally {
            loadingTable.value = false;
      table.isLoading = false;
        }
    }

    return {
        loadingTable,
    tableError,
        table,
        loadFiles,
    };
}