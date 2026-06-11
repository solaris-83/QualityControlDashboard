export interface StreamSubscription<T> {

    streamId: string

    next(chunk: T[], chunkIndex: number): void

    completed(): void

    error?(err: any): void
}