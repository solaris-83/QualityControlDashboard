class WebViewMessenger {

    private readonly defaultTimeoutMs = 10000

    private pending = new Map<string, {
        resolve: (result: any) => void
        reject: (reason?: unknown) => void
        timeout: ReturnType<typeof setTimeout>
    }>()

    constructor() {

        if ((window as any).chrome?.webview) {

            (window as any).chrome.webview.addEventListener(
                "message",
                (event: any) => {
                    this.onMessage(event.data)
                })
        }
    }

    request<T>(type: string, payload: any, timeoutMs = this.defaultTimeoutMs): Promise<T> {

        return new Promise<T>((resolve, reject) => {

            if (!(window as any).chrome?.webview) {
                reject(new Error("WebView messaging is unavailable."))
                return
            }

            const id = crypto.randomUUID()

            const timeout = setTimeout(() => {

                this.pending.delete(id)

                reject(new Error(`Request timed out: ${type}`))
            }, timeoutMs)

            this.pending.set(id, {
                resolve,                
                reject,
                timeout
            })

            ;(window as any).chrome.webview.postMessage({
                id,
                type,
                payload
            })
        })
    }

    publish(type: string, payload: any): void { 

        ;(window as any).chrome.webview.postMessage({
            id: crypto.randomUUID(),
            type,
            payload
        })
    }

    private onMessage(message: any): void {

        if (!message.isResponse)
            return

        console.log("Received response:", message.payload)

        const pending =
            this.pending.get(message.correlationId)

        if (!pending)
            return

        clearTimeout(pending.timeout)

        pending.resolve(message.payload)

        this.pending.delete(message.correlationId)
    }
}

export const bus = new WebViewMessenger()