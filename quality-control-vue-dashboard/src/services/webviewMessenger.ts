import { createId } from "../miscellanea/guid";

import { StreamChunk } from "../models/stream-chunk";
import { StreamSubscription } from "../models/streamSubscription";
import { WebMessage } from "../models/web-message";
import { TypeEnum } from "../models/type-enum";

type EventHandler<TResponse = any> = (payload: TResponse) => void;

type RequestResolver = (payload: any) => void;

interface PendingRequest {
  resolve: RequestResolver;

  reject: (reason?: any) => void;

  timeoutHandle: number;
}

export class WebViewMessenger {
  private readonly pendingRequests = new Map<string, PendingRequest>();

  private readonly eventHandlers = new Map<string, EventHandler<any>[]>();

  private readonly streamHandlers = new Map<string, StreamSubscription<any>>();

  constructor() {
    const webview = (window as any).chrome?.webview;

    if (!webview) {
      console.warn("Running outside WebView2");
      return;
    }

    webview.addEventListener("message", (e: any) => {
      this.onMessage(e.data);
    });
  }

  private publish(type: TypeEnum, payload?: any): void {
    const message: WebMessage = {
      id: createId(),
      type,
      payload,
      name: "",
      correlationId: "",
    };

    this.send(message);
  }

  public request<TResponse>(
    name: string,
    payload?: any,
    timeoutMs = 30000,
  ): Promise<TResponse> {
    return new Promise<TResponse>((resolve, reject) => {
      const id = createId();

      const timeoutHandle = window.setTimeout(() => {
        this.pendingRequests.delete(id);

        reject(new Error(`Timeout waiting for ${name}`));
      }, timeoutMs);

      this.pendingRequests.set(id, {
        resolve,
        reject,
        timeoutHandle,
      });

      const message: WebMessage = {
        id,
        type: TypeEnum.Request,
        payload,
        name,
        correlationId: "",
      };

      this.send(message);
    });
  }

  public subscribe<TResponse>(
    eventName: string,
    handler: EventHandler<TResponse>,
  ): () => void {
    if (!this.eventHandlers.has(eventName)) {
      this.eventHandlers.set(eventName, []);
    }

    this.eventHandlers.get(eventName)!.push(handler as EventHandler<TResponse>);

    return () => {
      const handlers = this.eventHandlers.get(eventName);

      if (!handlers) return;

      const index = handlers.indexOf(handler as EventHandler<TResponse>);

      if (index >= 0) {
        handlers.splice(index, 1);
      }
    };
  }

  public subscribeStream<T>(
    subscription: StreamSubscription<T>,
    payload: any,
  ): void {
    this.streamHandlers.set(
      subscription.streamId,
      subscription as StreamSubscription<any>,
    );

    const message: WebMessage = {
      id: createId(),
      type: TypeEnum.Stream,
      name: subscription.streamId,
      correlationId: "",
      payload: payload,
    };

    this.send(message);
  }

  public unsubscribeStream(streamId: string): void {
    this.streamHandlers.delete(streamId);
  }

  private send(message: WebMessage): void {
    const webview = (window as any).chrome?.webview;

    if (!webview) {
      console.warn("WebView2 not available");

      return;
    }

    webview.postMessage(message);
  }

  private onMessage(message: WebMessage): void {
    switch (message.type) {
      case TypeEnum.Request:
        this.handleRequest(message);
        break;
      case TypeEnum.Event:
        this.handleEvent(message);
        break;
      case TypeEnum.Stream:
        this.handleStream(message);
        break;
    }
  }

  private handleRequest(message: WebMessage): void {
    const request = this.pendingRequests.get(message.correlationId!);

    if (!request) return;

    clearTimeout(request.timeoutHandle);

    request.resolve(message.payload);

    this.pendingRequests.delete(message.correlationId!);
  }

  private handleEvent(message: WebMessage): void {
    const handlers = this.eventHandlers.get(message.name);

    if (!handlers) return;

    for (const handler of handlers) {
      handler(message.payload);
    }
  }

  private handleStream(message: WebMessage): void {
    const chunk = message.payload as StreamChunk<any>;

    if (!chunk) return;

    const handler = this.streamHandlers.get(message.name);

    if (!handler) return;

    if (chunk.items?.length)
      handler.next(chunk.items, chunk.chunkIndex);

    if (chunk.isLastChunk) {
      handler.completed();

      this.unsubscribeStream(message.name);
    }
  }
}

export const bus = new WebViewMessenger();
