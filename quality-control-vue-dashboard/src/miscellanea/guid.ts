export function createId(): string {

    if (crypto.randomUUID)
        return crypto.randomUUID()

    return `${Date.now()}-${Math.random()}`
}