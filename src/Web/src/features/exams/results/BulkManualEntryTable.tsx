import React, { useState, useEffect } from 'react';
import { Users, Plus, Trash2, AlertCircle, Info, CheckCircle2 } from 'lucide-react';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { ExamDetailDto } from '@/types/exam';
import { CreateBulkManualResultCommand, BulkManualResultResponse } from '@/types/examResultImport';
import { examService } from '@/services/examService';
import { apiClient } from '@/services/api/apiClient';

export const MAX_BULK_LIMIT = 500;

interface StudentOption {
  id: string;
  studentNumber: string;
  fullName: string;
}

interface BulkRowState {
  id: string; // unique row key
  studentId: string;
  sections: Record<string, { correct: number; wrong: number; blank: number }>;
}

interface BulkManualEntryTableProps {
  exam: ExamDetailDto;
  onSuccess: (result: BulkManualResultResponse) => void;
  onCancel: () => void;
}

export const BulkManualEntryTable: React.FC<BulkManualEntryTableProps> = ({
  exam,
  onSuccess,
  onCancel,
}) => {
  const [students, setStudents] = useState<StudentOption[]>([]);
  const [rows, setRows] = useState<BulkRowState[]>([]);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const createBlankRow = React.useCallback((studentId = ''): BulkRowState => {
    const secRecord: Record<string, { correct: number; wrong: number; blank: number }> = {};
    exam.sections.forEach((sec) => {
      secRecord[sec.id] = { correct: 0, wrong: 0, blank: sec.questionCount };
    });
    return {
      id: crypto.randomUUID(),
      studentId,
      sections: secRecord,
    };
  }, [exam]);

  useEffect(() => {
    const fetchStudents = async () => {
      try {
        const res = await apiClient.get<{ items: { id: string; studentNumber: string; firstName: string; lastName: string }[] }>(
          '/api/v1/students?pageSize=500'
        );
        if (res && res.items) {
          const list = res.items.map((s) => ({
            id: s.id,
            studentNumber: s.studentNumber,
            fullName: `${s.firstName} ${s.lastName}`,
          }));
          setStudents(list);

          // Seed 3 empty rows initially
          const initialRows: BulkRowState[] = list.slice(0, 3).map((st) => {
            const secRecord: Record<string, { correct: number; wrong: number; blank: number }> = {};
            exam.sections.forEach((sec) => {
              secRecord[sec.id] = { correct: 0, wrong: 0, blank: sec.questionCount };
            });
            return {
              id: crypto.randomUUID(),
              studentId: st.id,
              sections: secRecord,
            };
          });
          setRows(initialRows.length > 0 ? initialRows : [createBlankRow('')]);
        }
      } catch {
        setRows([createBlankRow('')]);
      }
    };

    fetchStudents();
  }, [exam, createBlankRow]);

  const handleAddRow = () => {
    if (rows.length >= MAX_BULK_LIMIT) return;

    // Find next unused student if available
    const usedStudentIds = new Set(rows.map((r) => r.studentId));
    const nextStudent = students.find((s) => !usedStudentIds.has(s.id));
    setRows((prev) => [...prev, createBlankRow(nextStudent ? nextStudent.id : '')]);
  };

  const handleRemoveRow = (rowId: string) => {
    if (rows.length <= 1) return;
    setRows((prev) => prev.filter((r) => r.id !== rowId));
  };

  const handleStudentSelect = (rowId: string, studentId: string) => {
    setRows((prev) =>
      prev.map((r) => (r.id === rowId ? { ...r, studentId } : r))
    );
  };

  const handleScoreChange = (
    rowId: string,
    sectionId: string,
    field: 'correct' | 'wrong' | 'blank',
    val: number
  ) => {
    const cleanVal = Math.max(0, Math.floor(isNaN(val) ? 0 : val));
    setRows((prev) =>
      prev.map((r) => {
        if (r.id !== rowId) return r;

        const currentSec = r.sections[sectionId] || { correct: 0, wrong: 0, blank: 0 };
        const updatedSec = { ...currentSec, [field]: cleanVal };

        const secObj = exam.sections.find((s) => s.id === sectionId);
        if (secObj && (field === 'correct' || field === 'wrong')) {
          const remaining = secObj.questionCount - (field === 'correct' ? cleanVal + updatedSec.wrong : updatedSec.correct + cleanVal);
          updatedSec.blank = Math.max(0, remaining);
        }

        return {
          ...r,
          sections: {
            ...r.sections,
            [sectionId]: updatedSec,
          },
        };
      })
    );
  };

  // Validation Checks
  const isRowValid = (row: BulkRowState): boolean => {
    if (!row.studentId) return false;
    return exam.sections.every((sec) => {
      const s = row.sections[sec.id] || { correct: 0, wrong: 0, blank: 0 };
      return s.correct + s.wrong + s.blank === sec.questionCount;
    });
  };

  const hasDuplicateStudents = (): boolean => {
    const ids = rows.map((r) => r.studentId).filter(Boolean);
    return new Set(ids).size !== ids.length;
  };

  const allRowsValid = rows.length > 0 && rows.every(isRowValid) && !hasDuplicateStudents() && rows.length <= MAX_BULK_LIMIT;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!allRowsValid) return;

    setErrorMessage(null);
    setIsSubmitting(true);

    try {
      const command: CreateBulkManualResultCommand = {
        studentResults: rows.map((r) => ({
          studentId: r.studentId,
          sections: exam.sections.map((sec) => {
            const s = r.sections[sec.id] || { correct: 0, wrong: 0, blank: 0 };
            return {
              examSectionId: sec.id,
              correct: s.correct,
              wrong: s.wrong,
              blank: s.blank,
            };
          }),
        })),
      };

      const res = await examService.createBulkManualResults(exam.id, command);
      onSuccess(res);
    } catch (err: unknown) {
      setErrorMessage(
        err instanceof Error ? err.message : 'Toplu manuel sonuçlar kaydedilirken bir hata oluştu.'
      );
      // NOTE: Entered row values are preserved in state so user can fix and retry!
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="flex items-center justify-between border-b pb-4 dark:border-gray-800">
        <div>
          <h2 className="text-xl font-bold text-gray-900 dark:text-white flex items-center gap-2">
            <Users className="h-5 w-5 text-navy-600" />
            Toplu Hızlı Tablo Sonuç Girişi
          </h2>
          <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
            {exam.title} ({exam.code}) — Maksimum {MAX_BULK_LIMIT} öğrenci
          </p>
        </div>
        <Button type="button" variant="outline" size="sm" onClick={onCancel}>
          Vazgeç
        </Button>
      </div>

      {/* All-or-Nothing Disclaimer */}
      <div className="p-4 rounded-xl bg-amber-50 dark:bg-amber-950/40 border border-amber-200 dark:border-amber-900 text-amber-900 dark:text-amber-200 text-xs flex items-start gap-3">
        <Info className="h-5 w-5 shrink-0 text-amber-600 dark:text-amber-400 mt-0.5" />
        <div>
          <div className="font-semibold text-sm">Tümü ya da Hiçbiri (All-or-Nothing) Kuralı</div>
          <div>
            Herhangi bir öğrencinin sonucunda hata bulunursa <strong>hiçbir kayıt oluşturulmaz</strong>. Hata durumunda girdiğiniz veriler korunur, düzelttikten sonra tekrar gönderebilirsiniz.
          </div>
        </div>
      </div>

      {errorMessage && (
        <div className="p-4 rounded-xl bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-900 text-red-700 dark:text-red-300 text-xs flex items-center gap-2">
          <AlertCircle className="h-4 w-4 shrink-0" />
          <span>{errorMessage}</span>
        </div>
      )}

      {hasDuplicateStudents() && (
        <div className="p-3 rounded-lg bg-red-100 dark:bg-red-900/40 text-red-800 dark:text-red-300 text-xs flex items-center gap-2">
          <AlertCircle className="h-4 w-4" />
          <span>Tabloda aynı öğrenci birden fazla kez seçilmiş! Lütfen mükerrer öğrencileri kaldırın.</span>
        </div>
      )}

      {/* Table Container */}
      <Card className="overflow-hidden border border-gray-200 dark:border-gray-800">
        <div className="p-4 bg-gray-50 dark:bg-navy-900 border-b border-gray-200 dark:border-gray-800 flex items-center justify-between">
          <span className="text-xs font-semibold text-gray-700 dark:text-gray-300">
            Girilen Öğrenci Sayısı: {rows.length} / {MAX_BULK_LIMIT}
          </span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleAddRow}
            disabled={rows.length >= MAX_BULK_LIMIT}
            className="flex items-center gap-1.5"
          >
            <Plus className="h-4 w-4" /> Öğrenci Satırı Ekle
          </Button>
        </div>

        <div className="overflow-x-auto max-h-[550px] overflow-y-auto">
          <table className="w-full text-xs text-left border-collapse">
            <thead className="sticky top-0 z-20 bg-gray-100 dark:bg-navy-950 text-gray-700 dark:text-gray-300 uppercase shadow-sm">
              {/* Grouped Subject Headers */}
              <tr>
                <th className="sticky left-0 z-30 bg-gray-100 dark:bg-navy-950 px-4 py-2 border-b border-r border-gray-300 dark:border-gray-700 min-w-[220px]">
                  Öğrenci
                </th>
                {exam.sections.map((sec) => (
                  <th
                    key={sec.id}
                    colSpan={3}
                    className="px-2 py-2 text-center border-b border-r border-gray-300 dark:border-gray-700 bg-gray-200/70 dark:bg-navy-900"
                  >
                    {sec.subjectName} ({sec.questionCount} Soru)
                  </th>
                ))}
                <th className="px-3 py-2 text-center border-b border-gray-300 dark:border-gray-700">Durum</th>
                <th className="px-2 py-2 text-center border-b border-gray-300 dark:border-gray-700">İşlem</th>
              </tr>
              {/* Score Sub-headers D / Y / B */}
              <tr>
                <th className="sticky left-0 z-30 bg-gray-100 dark:bg-navy-950 px-4 py-1 border-b border-r border-gray-300 dark:border-gray-700">
                  Adı Soyadı
                </th>
                {exam.sections.map((sec) => (
                  <React.Fragment key={`sub-${sec.id}`}>
                    <th className="px-1 py-1 text-center border-b border-gray-300 dark:border-gray-700 text-emerald-600 dark:text-emerald-400 w-16">D</th>
                    <th className="px-1 py-1 text-center border-b border-gray-300 dark:border-gray-700 text-red-600 dark:text-red-400 w-16">Y</th>
                    <th className="px-1 py-1 text-center border-b border-r border-gray-300 dark:border-gray-700 text-gray-500 w-16">B</th>
                  </React.Fragment>
                ))}
                <th className="px-3 py-1 text-center border-b border-gray-300 dark:border-gray-700">Kontrol</th>
                <th className="px-2 py-1 text-center border-b border-gray-300 dark:border-gray-700">Sil</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 dark:divide-gray-800">
              {rows.map((row, idx) => {
                const valid = isRowValid(row);

                return (
                  <tr key={row.id} className="hover:bg-gray-50 dark:hover:bg-navy-900/40">
                    {/* Frozen Student Dropdown */}
                    <td className="sticky left-0 z-10 bg-white dark:bg-navy-950 px-3 py-2 border-r border-gray-300 dark:border-gray-700">
                      <select
                        value={row.studentId}
                        onChange={(e) => handleStudentSelect(row.id, e.target.value)}
                        className="w-full text-xs rounded-lg border border-gray-300 dark:border-gray-700 bg-transparent px-2 py-1 focus:ring-1 focus:ring-navy-500 focus:outline-none"
                      >
                        <option value="">-- Öğrenci Seç ({idx + 1}) --</option>
                        {students.map((st) => (
                          <option key={st.id} value={st.id}>
                            {st.fullName} ({st.studentNumber || 'No yok'})
                          </option>
                        ))}
                      </select>
                    </td>

                    {/* Section Scores Inputs */}
                    {exam.sections.map((sec) => {
                      const secState = row.sections[sec.id] || { correct: 0, wrong: 0, blank: sec.questionCount };

                      return (
                        <React.Fragment key={`input-${row.id}-${sec.id}`}>
                          <td className="p-1">
                            <input
                              type="number"
                              min={0}
                              max={sec.questionCount}
                              value={secState.correct}
                              onChange={(e) => handleScoreChange(row.id, sec.id, 'correct', parseInt(e.target.value, 10))}
                              className="w-full text-center text-xs p-1 rounded border border-gray-300 dark:border-gray-700 bg-white dark:bg-navy-900 font-medium focus:ring-1 focus:ring-emerald-500 focus:outline-none"
                            />
                          </td>
                          <td className="p-1">
                            <input
                              type="number"
                              min={0}
                              max={sec.questionCount}
                              value={secState.wrong}
                              onChange={(e) => handleScoreChange(row.id, sec.id, 'wrong', parseInt(e.target.value, 10))}
                              className="w-full text-center text-xs p-1 rounded border border-gray-300 dark:border-gray-700 bg-white dark:bg-navy-900 font-medium focus:ring-1 focus:ring-red-500 focus:outline-none"
                            />
                          </td>
                          <td className="p-1 border-r border-gray-200 dark:border-gray-800">
                            <input
                              type="number"
                              min={0}
                              max={sec.questionCount}
                              value={secState.blank}
                              onChange={(e) => handleScoreChange(row.id, sec.id, 'blank', parseInt(e.target.value, 10))}
                              className="w-full text-center text-xs p-1 rounded border border-gray-300 dark:border-gray-700 bg-white dark:bg-navy-900 font-medium focus:ring-1 focus:ring-gray-500 focus:outline-none"
                            />
                          </td>
                        </React.Fragment>
                      );
                    })}

                    {/* Row Validation State */}
                    <td className="px-3 py-2 text-center">
                      {valid ? (
                        <span className="text-emerald-600 dark:text-emerald-400 font-medium inline-flex items-center gap-1">
                          <CheckCircle2 className="h-4 w-4" /> Geçerli
                        </span>
                      ) : (
                        <span className="text-red-500 font-medium inline-flex items-center gap-1" title="Öğrenci seçilmeli ve tüm derslerde soru sayıları eşleşmelidir.">
                          <AlertCircle className="h-4 w-4" /> Eksik
                        </span>
                      )}
                    </td>

                    {/* Remove Action */}
                    <td className="px-2 py-2 text-center">
                      <button
                        type="button"
                        onClick={() => handleRemoveRow(row.id)}
                        disabled={rows.length <= 1}
                        className="text-gray-400 hover:text-red-600 disabled:opacity-30 transition-colors"
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </Card>

      {/* Footer Controls */}
      <div className="flex items-center justify-between pt-4">
        <Button
          type="button"
          variant="outline"
          onClick={handleAddRow}
          disabled={rows.length >= MAX_BULK_LIMIT}
          className="flex items-center gap-1.5 text-xs"
        >
          <Plus className="h-4 w-4" /> Satır Ekle ({rows.length}/{MAX_BULK_LIMIT})
        </Button>

        <div className="flex items-center gap-3">
          <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
            Vazgeç
          </Button>
          <Button
            type="submit"
            variant="primary"
            disabled={!allRowsValid || isSubmitting}
            isLoading={isSubmitting}
          >
            {rows.length} Öğrencinin Sonucunu Toplu Kaydet
          </Button>
        </div>
      </div>
    </form>
  );
};
