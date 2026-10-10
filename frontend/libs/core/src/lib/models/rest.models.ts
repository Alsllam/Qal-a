export interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
}

export type ActiveFilter = 'All' | 'Active' | 'InActive';

export interface BaseFilterDto {
  filterText?: string;
  skipCount: number;
  maxResultCount: number;
  sorting?: string;
  activeFilter?: ActiveFilter;
}

export interface EntityIdDto {
  id: string;
}

export interface PagedRequestDto {
  skipCount: number;
  maxResultCount: number;
}

/** Backend error envelope: `{ error: { code, messages[], source } }`. */
export interface RemoteErrorDto {
  error?: { code?: string; messages?: string[]; source?: string };
}
