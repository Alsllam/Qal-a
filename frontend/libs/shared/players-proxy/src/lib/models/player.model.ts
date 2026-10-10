import { BaseFilterDto, PagedRequestDto } from '@qala-fe/Core';

export interface PlayerListDto {
  id: string;
  displayName: string;
  email?: string;
  /** Glicko-2 rating (starts at 1500). */
  rating: number;
  /** Glicko-2 rating deviation (starts at 350). */
  ratingDeviation: number;
  gamesPlayed: number;
  wins: number;
  draws: number;
  losses: number;
  /** `false` means banned. */
  isActive: boolean;
  locale: 'ar' | 'en';
  creationTime: string;
  lastSeenTime?: string;
}

export interface PlayerDto extends PlayerListDto {
  avatarId?: string;
  volatility?: number;
}

export interface PlayerProfileDto {
  id: string;
  displayName: string;
  avatarId?: string;
  locale: 'ar' | 'en';
  rating: number;
  ratingDeviation: number;
}

export interface UpdateMyProfileDto {
  displayName: string;
  avatarId?: string;
  locale: 'ar' | 'en';
}

export interface FilterPlayerDto extends BaseFilterDto {
  minRating?: number;
  maxRating?: number;
}

export type LeaderboardRequestDto = PagedRequestDto;

export interface LeaderboardEntryDto {
  rank: number;
  displayName: string;
  rating: number;
}
