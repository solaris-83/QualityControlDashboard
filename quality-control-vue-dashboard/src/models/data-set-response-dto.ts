/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

export class DataSetResponseDto {
    id: number;
    week: number;
    year: number;
    license: string = "";
    vin: string = "";
    model: string = "";
    appName: string = "";
    resultType: string = "";
    errorCode: string | null;
    elapsedTime: number | null;
    affectedControllers: string = "";
}
