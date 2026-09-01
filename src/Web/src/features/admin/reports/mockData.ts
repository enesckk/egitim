import { AdminReportsViewModel } from './types';

export const initialAdminReportsData: AdminReportsViewModel = {
  branchComparisons: [
    {
      branchName: 'Kadıköy Şubesi',
      studentCount: 142,
      coachCount: 6,
      averageAdherence: 84,
      averageNet: 26.4,
    },
    {
      branchName: 'Şişli Şubesi',
      studentCount: 118,
      coachCount: 5,
      averageAdherence: 79,
      averageNet: 24.1,
    },
    {
      branchName: 'Ümraniye Şubesi',
      studentCount: 96,
      coachCount: 4,
      averageAdherence: 76,
      averageNet: 22.8,
    },
    {
      branchName: 'Bakırköy Şubesi',
      studentCount: 67,
      coachCount: 3,
      averageAdherence: 71,
      averageNet: 21.5,
    },
  ],
  adherenceDistribution: [
    {
      range: 'Yüksek Uyuma Sahip (≥%80)',
      studentCount: 265,
      percentage: 63,
      color: 'bg-success',
    },
    {
      range: 'Orta Düzey Uyum (%50–%79)',
      studentCount: 127,
      percentage: 30,
      color: 'bg-warning',
    },
    {
      range: 'Kritik Düşük Uyum (<%50)',
      studentCount: 31,
      percentage: 7,
      color: 'bg-danger',
    },
  ],
};
