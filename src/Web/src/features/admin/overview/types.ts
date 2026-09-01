export type Timeframe = '1m' | '3m' | '6m';

export interface TytTrendPoint {
  ay: string;
  turkce: number;
  matematik: number;
  sosyal: number;
  fen: number;
}

export interface InstitutionKpiItem {
  label: string;
  value: string;
  sub: string;
  color: string;
  iconType: 'users' | 'coaches' | 'adherence' | 'clock';
}

export interface AdminCoachPortfolioItem {
  id: string;
  name: string;
  initials: string;
  students: number;
  adherence: number;
  avgStudy: number;
  attention: number;
  status: 'good' | 'attention';
}

export interface AttentionStatItem {
  label: string;
  value: string;
  sub: string;
}

export interface InstitutionOverviewViewModel {
  institutionName: string;
  branchCount: number;
  kpis: InstitutionKpiItem[];
  trendData: Record<Timeframe, TytTrendPoint[]>;
  attentionStats: AttentionStatItem[];
  coaches: AdminCoachPortfolioItem[];
}
