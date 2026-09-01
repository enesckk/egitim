export interface BranchComparisonItem {
  branchName: string;
  studentCount: number;
  coachCount: number;
  averageAdherence: number;
  averageNet: number;
}

export interface AdherenceDistributionItem {
  range: string;
  studentCount: number;
  percentage: number;
  color: string;
}

export interface AdminReportsViewModel {
  branchComparisons: BranchComparisonItem[];
  adherenceDistribution: AdherenceDistributionItem[];
}
