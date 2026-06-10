export interface StreamSubscription<T> {

    streamId: string

    next(chunk: T[]): void

    completed(): void

    error?(err: any): void
}