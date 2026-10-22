import React, { useState, useEffect } from 'react';
import { X, CheckCircle2, AlertTriangle, XCircle, UserCheck } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { AdminStudentDirectoryItem } from '../types';
import { adminService, CoachSummaryDto } from '@/services/adminService';

export interface StudentModalProps {
  student: AdminStudentDirectoryItem;
  onClose: () => void;
  onStudentUpdated?: () => void;
}

export const StudentModal: React.FC<StudentModalProps> = ({
  student,
  onClose,
  onStudentUpdated,
}) => {
  const [coaches, setCoaches] = useState<CoachSummaryDto[]>([]);
  const [selectedCoachId, setSelectedCoachId] = useState<string>('');
  const [isAssigning, setIsAssigning] = useState(false);
  const [assignSuccess, setAssignSuccess] = useState(false);
  const [assignError, setAssignError] = useState<string | null>(null);

  useEffect(() => {
    adminService
      .listCoaches({ page: 1, pageSize: 50 })
      .then((res) => {
        setCoaches(res?.items || []);
      })
      .catch(() => {
        // Ignored in offline/mock mode
      });
  }, []);

  const handleAssignCoach = async () => {
    if (!selectedCoachId) return;
    setIsAssigning(true);
    setAssignError(null);
    setAssignSuccess(false);

    try {
      await adminService.assignCoach(student.id, selectedCoachId);
      setAssignSuccess(true);
      if (onStudentUpdated) {
        onStudentUpdated();
      }
    } catch (err: unknown) {
      setAssignError(err instanceof Error ? err.message : 'Koç ataması başarısız oldu.');
    } finally {
      setIsAssigning(false);
    }
  };

  const getStatusBadge = (status: AdminStudentDirectoryItem['status']) => {
    switch (status) {
      case 'takipte':
        return {
          label: 'Takipte',
          icon: <CheckCircle2 className="h-3.5 w-3.5 text-success" />,
          badge: 'bg-success-light text-success-dark border-green-200',
        };
      case 'dikkat':
        return {
          label: 'Dikkat',
          icon: <AlertTriangle className="h-3.5 w-3.5 text-warning" />,
          badge: 'bg-warning-light text-warning-dark border-amber-200',
        };
      case 'kritik':
        return {
          label: 'Kritik',
          icon: <XCircle className="h-3.5 w-3.5 text-danger" />,
          badge: 'bg-danger-light text-danger-dark border-red-200',
        };
    }
  };

  const statusConfig = getStatusBadge(student.status);

  return (
    <div className="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-4">
      <div
        className="absolute inset-0 bg-navy-950/40 backdrop-blur-xs transition-opacity"
        onClick={onClose}
      />
      <div className="relative w-full max-w-md bg-white rounded-2xl shadow-2xl overflow-hidden select-none border border-neutral-100">
        {/* Header */}
        <div className="bg-navy-900 px-6 py-5 text-white">
          <div className="flex items-start justify-between">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-full bg-navy-700 text-white text-sm font-semibold flex items-center justify-center flex-shrink-0">
                {student.initials}
              </div>
              <div className="min-w-0">
                <p className="text-white font-semibold leading-tight truncate">{student.name}</p>
                <p className="text-navy-300 text-xs mt-0.5">
                  {student.grade}. Sınıf · {student.exam} · {student.field}
                </p>
              </div>
            </div>
            <button
              type="button"
              onClick={onClose}
              className="text-navy-300 hover:text-white p-1 rounded-lg hover:bg-navy-800 transition-colors"
              aria-label="Kapat"
            >
              <X className="h-4 w-4" />
            </button>
          </div>
        </div>

        {/* Body */}
        <div className="p-6 space-y-4">
          <div className="flex items-center gap-3">
            <span
              className={cn(
                'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold border',
                statusConfig.badge
              )}
            >
              {statusConfig.icon}
              {statusConfig.label}
            </span>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="bg-surface-alt rounded-xl p-3 border border-neutral-100">
              <p className="text-xs text-neutral-400 mb-1">Plan Uyumu</p>
              <p
                className={cn(
                  'font-mono text-xl font-bold',
                  student.planAdherence >= 80
                    ? 'text-success'
                    : student.planAdherence >= 50
                    ? 'text-warning-dark'
                    : 'text-danger'
                )}
              >
                %{student.planAdherence}
              </p>
            </div>
            <div className="bg-surface-alt rounded-xl p-3 border border-neutral-100">
              <p className="text-xs text-neutral-400 mb-1">Son Aktivite</p>
              <p className="text-sm font-semibold text-neutral-800">{student.lastActivity}</p>
            </div>
          </div>

          <div className="space-y-1.5 text-xs text-neutral-600 bg-neutral-50 p-3 rounded-xl">
            {student.coach && (
              <div>
                <span className="text-neutral-400">Atanmış Koç:</span>{' '}
                <strong className="text-neutral-800">{student.coach}</strong>
              </div>
            )}
            {student.branch && (
              <div>
                <span className="text-neutral-400">Kayıtlı Şube:</span>{' '}
                <strong className="text-neutral-800">{student.branch} Şubesi</strong>
              </div>
            )}
          </div>

          {/* Coach Assignment Section */}
          <div className="pt-2 border-t border-neutral-100 space-y-2">
            <label className="text-xs font-semibold text-neutral-700 block">
              Koç Ata veya Değiştir
            </label>
            <div className="flex items-center gap-2">
              <select
                value={selectedCoachId}
                onChange={(e) => setSelectedCoachId(e.target.value)}
                className="flex-1 px-3 py-2 text-xs rounded-xl border border-neutral-200 bg-white text-neutral-800 focus:outline-none focus:ring-2 focus:ring-primary-500"
              >
                <option value="">Koç seçiniz...</option>
                {coaches.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.fullName || `${c.firstName} ${c.lastName}`} ({c.assignedStudentCount} öğrenci)
                  </option>
                ))}
              </select>
              <Button
                variant="primary"
                size="sm"
                onClick={handleAssignCoach}
                disabled={!selectedCoachId || isAssigning}
                leftIcon={<UserCheck className="h-3.5 w-3.5" />}
              >
                {isAssigning ? 'Atanıyor...' : 'Ata'}
              </Button>
            </div>
            {assignSuccess && (
              <p className="text-xs text-success font-medium">Koç başarıyla atandı.</p>
            )}
            {assignError && (
              <p className="text-xs text-danger font-medium">{assignError}</p>
            )}
          </div>

          <div className="pt-2">
            <Button
              variant="secondary"
              size="md"
              className="w-full"
              onClick={onClose}
            >
              Kapat
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
};
