import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
  id: number;
  type: ToastType;
  /** Translation key, or plain text when `raw` is true (backend messages). */
  message: string;
  params?: Record<string, unknown>;
  raw?: boolean;
  life: number;
}

@Injectable({ providedIn: 'root' })
export class ToasterService {
  private nextId = 1;
  readonly toasts = signal<Toast[]>([]);

  success(message: string, params?: Record<string, unknown>) {
    return this.show({ type: 'success', message, params });
  }
  error(message: string, params?: Record<string, unknown>, raw = false) {
    return this.show({ type: 'error', message, params, raw, life: 6000 });
  }
  warning(message: string, params?: Record<string, unknown>) {
    return this.show({ type: 'warning', message, params });
  }
  info(message: string, params?: Record<string, unknown>) {
    return this.show({ type: 'info', message, params });
  }

  show(toast: Omit<Toast, 'id' | 'life'> & { life?: number }): number {
    const id = this.nextId++;
    const life = toast.life ?? 4000;
    this.toasts.update((list) => [...list, { ...toast, id, life }].slice(-5));
    setTimeout(() => this.remove(id), life);
    return id;
  }

  remove(id: number): void {
    this.toasts.update((list) => list.filter((t) => t.id !== id));
  }
}
