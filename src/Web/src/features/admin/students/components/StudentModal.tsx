import React, { useState, useEffect } from 'react';
import {
  X,
  UserCheck,
  CheckCircle2,
  AlertCircle,
  Building2,
  Calendar,
  GraduationCap,
  Users,
} from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { AdminStudentDirectoryItem } from '../types';
import { adminService, CoachSummaryDto } from '@/services/adminService';
import { academicService, Student360Dto } from '@/services/academicService';

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

  const [student360, setStudent360] = useState<Student360Dto | null>(null);
  const [isLoading360, setIsLoading360] = useState(true);

  useEffect(() => {
    let active = true;
    setIsLoading360(true);

    Promise.all([
      adminService.listCoaches({ page: 1, pageSize: 50 }).catch(() => null),
      academicService.getStudent360(student.id).catch(() => null),
    ]).then(([coachRes, s360]) => {
      if (!active) return;
      if (coachRes?.items) setCoaches(coachRes.items);
      if (s360) setStudent360(s360);
      setIsLoading360(false);
    });

    return () => {
      active = false;
    };
  }, [student.id]);

  const handleAssignCoach = async () => {
    if (!selectedCoachId || isAssigning) return;
    setIsAssigning(true);
    setAssignError(null);
    setAssignSuccess(false);

    try {
      await adminService.assignCoach(student.id, selectedCoachId);
      setAssignSuccess(true);
      const updated360 = await academicService.getStudent360(student.id).catch(() => null);
      if (updated360) setStudent360(updated360);
      if (onStudentUpdated) {
        onStudentUpdated();
      }
    } catch (err: unknown) {
      setAssignError(err instanceof Error ? err.message : 'Koç ataması başarısız oldu.');
    } finally {
      setIsAssigning(false);
    }
  };

  const assignedCoach = student360?.coaches?.find((c) => c.isPrimary) || student360?.coaches?.[0];
  const linkedParent = student360?.parents?.[0];

  return (
    <div className="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-4">
      <div
        className="absolute inset-0 bg-navy-950/40 backdrop-blur-xs transition-opacity"
        onClick={onClose}
      />
      <div className="relative w-full max-w-lg bg-white rounded-3xl shadow-2xl overflow-hidden select-none border border-neutral-200 animate-fadeIn">
        {/* Header Banner */}
        <div className="bg-[#14233E] px-6 py-5 text-white">
          <div className="flex items-start justify-between">
            <div className="flex items-center gap-3">
              <div className="w-12 h-12 rounded-2xl bg-gradient-to-br from-primary-500 to-primary-700 text-white text-base font-bold flex items-center justify-center shrink-0 shadow-soft-xs">
                {student.initials}
              </div>
              <div className="min-w-0">
                <p className="text-white font-serif font-bold text-lg leading-tight truncate">
                  {student.name}
                </p>
                <p className="text-neutral-300 text-xs mt-0.5 font-medium">
                  {student.grade} · {student.exam}
                  {student.studentNumber ? ` · No: #${student.studentNumber}` : ''}
                </p>
              </div>
            </div>
            <button
              type="button"
              onClick={onClose}
              className="text-neutral-400 hover:text-white p-1.5 rounded-xl hover:bg-white/10 transition-colors"
              aria-label="Kapat"
            >
              <X className="h-5 w-5" />
            </button>
          </div>
        </div>

        {/* Modal Body */}
        <div className="p-6 space-y-4 max-h-[75vh] overflow-y-auto">
          {/* Status & School Card */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="bg-[#F8F7F3] p-3.5 rounded-2xl border border-neutral-200/80">
              <span className="text-[11px] font-bold text-neutral-400 uppercase tracking-wider block">
                Öğrenci Durumu
              </span>
              <div className="flex items-center gap-2 mt-1">
                <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-bold bg-emerald-50 text-emerald-700 border border-emerald-200">
                  <CheckCircle2 className="h-3 w-3" />
                  {student.status === 'Active' ? 'Aktif Kayıt' : student.status}
                </span>
              </div>
            </div>

            <div className="bg-[#F8F7F3] p-3.5 rounded-2xl border border-neutral-200/80">
              <span className="text-[11px] font-bold text-neutral-400 uppercase tracking-wider block">
                Kayıt Tarihi
              </span>
              <p className="text-xs font-semibold text-neutral-800 mt-1 flex items-center gap-1.5">
                <Calendar className="h-3.5 w-3.5 text-neutral-400" />
                {student.createdAt
                  ? new Date(student.createdAt).toLocaleDateString('tr-TR', {
                      day: 'numeric',
                      month: 'long',
                      year: 'numeric',
                    })
                  : '—'}
              </p>
            </div>
          </div>

          {/* School & Parent Info */}
          <div className="space-y-2 text-xs text-neutral-700 bg-neutral-50/80 p-4 rounded-2xl border border-neutral-200/70">
            <div className="flex items-center justify-between pb-2 border-b border-neutral-200/60">
              <span className="text-neutral-400 font-medium flex items-center gap-1.5">
                <Building2 className="h-3.5 w-3.5 text-neutral-400" />
                Kayıtlı Okul:
              </span>
              <span className="font-semibold text-neutral-800">
                {student.schoolName || 'Kurum İçi Öğrenci'}
              </span>
            </div>

            <div className="flex items-center justify-between pb-2 border-b border-neutral-200/60">
              <span className="text-neutral-400 font-medium flex items-center gap-1.5">
                <GraduationCap className="h-3.5 w-3.5 text-neutral-400" />
                Danışman Koç:
              </span>
              <span className="font-semibold text-neutral-800">
                {isLoading360 ? 'Yükleniyor...' : assignedCoach ? assignedCoach.name : 'Atanmamış'}
              </span>
            </div>

            <div className="flex items-center justify-between">
              <span className="text-neutral-400 font-medium flex items-center gap-1.5">
                <Users className="h-3.5 w-3.5 text-neutral-400" />
                Bağlı Veli:
              </span>
              <span className="font-semibold text-neutral-800">
                {isLoading360
                  ? 'Yükleniyor...'
                  : linkedParent
                  ? `${linkedParent.name} (${linkedParent.relationshipType || 'Veli'})`
                  : 'Veli Tanımlanmamış'}
              </span>
            </div>
          </div>

          {/* Coach Assignment Section */}
          <div className="pt-3 border-t border-neutral-100 space-y-2.5">
            <label className="text-xs font-bold text-navy-950 uppercase tracking-wider block">
              Danışman Koç Ata / Değiştir
            </label>
            <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-2">
              <select
                value={selectedCoachId}
                onChange={(e) => setSelectedCoachId(e.target.value)}
                className="flex-1 px-3.5 py-2.5 text-xs rounded-xl border border-neutral-200 bg-white text-neutral-800 focus:outline-none focus:ring-2 focus:ring-primary-500 font-medium"
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
                leftIcon={<UserCheck className="h-4 w-4" />}
                className="bg-[#315FE8] hover:bg-blue-700 rounded-xl font-semibold h-10 px-4"
              >
                {isAssigning ? 'Atanıyor...' : 'Ata'}
              </Button>
            </div>

            {assignSuccess && (
              <p className="text-xs text-emerald-700 bg-emerald-50 border border-emerald-200 p-2.5 rounded-xl font-medium flex items-center gap-1.5">
                <CheckCircle2 className="h-4 w-4 shrink-0" />
                Koç ataması başarıyla güncellendi.
              </p>
            )}

            {assignError && (
              <p className="text-xs text-rose-700 bg-rose-50 border border-rose-200 p-2.5 rounded-xl font-medium flex items-center gap-1.5">
                <AlertCircle className="h-4 w-4 shrink-0" />
                {assignError}
              </p>
            )}
          </div>

          <div className="pt-3 border-t border-neutral-100">
            <Button
              variant="secondary"
              size="md"
              className="w-full rounded-xl font-semibold"
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

export default StudentModal;
