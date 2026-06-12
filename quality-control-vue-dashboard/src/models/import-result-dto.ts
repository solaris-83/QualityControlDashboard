/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { TimeSpan } from "./time-span";

export class ImportResultDto {
    success: boolean;
    totalRecordsProcessed: number;
    recordsImported: number;
    recordsSkipped: number;
    errorMessage: string | null;
    startTime: Date;
    endTime: Date;
    duration: TimeSpan;
    fullName: string;
}
