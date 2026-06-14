import { ErrorDto } from "../models/error-dto";

export class ErrorService {

    show(error: ErrorDto)
    {
        if (error && error.message) {
            alert(
                error.message)
        }
    }
}