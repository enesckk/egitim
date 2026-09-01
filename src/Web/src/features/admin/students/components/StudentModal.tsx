import React from 'react';
import { X, CheckCircle2, AlertTriangle, XCircle } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { AdminStudentDirectoryItem } from '../types';

export interface StudentModalProps {
  student: AdminStudentDirectoryItem;
  onClose: () => void;
}

export const StudentModal: React.FC<StudentModalProps> = ({ student, onClose }) => {
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
