import React, { useState, useEffect } from 'react';
import { User, AlertCircle, CheckCircle2, Calculator, Info } from 'lucide-react';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { ExamDetailDto, ExamSectionDetailDto } from '@/types/exam';
import { CreateSingleManualResultCommand, SingleManualResultResponse } from '@/types/examResultImport';
import { examService } from '@/services/examService';
import { apiClient } from '@/services/api/apiClient';

interface StudentOption {
  id: string;
  studentNumber: string;
  fullName: string;
}

interface SectionInputState {
  correct: number;
  wrong: number;
  blank: number;
}

interface SingleManualEntryFormProps {
  exam: ExamDetailDto;
  onSuccess: (result: SingleManualResultResponse) => void;
  onCancel: () => void;
}

export const SingleManualEntryForm: React.FC<SingleManualEntryFormProps> = ({
  exam,
  onSuccess,
  onCancel,
}) => {
  const [students, setStudents] = useState<StudentOption[]>([]);
  const [selectedStudentId, setSelectedStudentId] = useState<string>('');
  const [loadingStudents, setLoadingStudents] = useState<boolean>(true);

  const [sectionInputs, setSectionInputs] = useState<Record<string, SectionInputState>>({});
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    const fetchStudents = async () => {
      try {
        setLoadingStudents(true);
        const res = await apiClient.get<{ items: { id: string; studentNumber: string; firstName: string; lastName: string }[] }>(
          '/api/v1/students?pageSize=500'
        );
        if (res && res.items) {
          setStudents(
            res.items.map((s) => ({
              id: s.id,
              studentNumber: s.studentNumber,
              fullName: `${s.firstName} ${s.lastName}`,
            }))
          );
        }
      } catch {
        // Fallback option if students fetch is forbidden or empty
      } finally {
        setLoadingStudents(false);
      }
    };

    fetchStudents();

    // Initialize section input state
    const initialInputs: Record<string, SectionInputState> = {};
    exam.sections.forEach((sec) => {
      initialInputs[sec.id] = {
        correct: 0,
        wrong: 0,
        blank: sec.questionCount,
      };
    });
    setSectionInputs(initialInputs);
  }, [exam]);

  const handleScoreChange = (sectionId: string, field: 'correct' | 'wrong' | 'blank', value: number) => {
    const cleanValue = Math.max(0, Math.floor(isNaN(value) ? 0 : value));
    setSectionInputs((prev) => {
      const current = prev[sectionId] || { correct: 0, wrong: 0, blank: 0 };
      const updated = { ...current, [field]: cleanValue };

      // Auto-adjust blank if user updates correct or wrong to make totals convenient
      if (field === 'correct' || field === 'wrong') {
        const sec = exam.sections.find((s) => s.id === sectionId);
        if (sec) {
          const remaining = sec.questionCount - (field === 'correct' ? cleanValue + updated.wrong : updated.correct + cleanValue);
          updated.blank = Math.max(0, remaining);
        }
      }

      return { ...prev, [sectionId]: updated };
    });
  };

  const calculateNet = (sec: ExamSectionDetailDto, input: SectionInputState): number => {
    const penalty = sec.effectiveWrongAnswerPenalty || exam.wrongAnswerPenalty || 4.0;
    if (penalty <= 0) return input.correct;
    const net = input.correct - input.wrong / penalty;
    return Math.round(net * 100) / 100;
  };

  const isSectionValid = (sec: ExamSectionDetailDto): boolean => {
    const input = sectionInputs[sec.id] || { correct: 0, wrong: 0, blank: 0 };
    return input.correct + input.wrong + input.blank === sec.questionCount;
  };

  const allSectionsValid = exam.sections.every(isSectionValid) && Boolean(selectedStudentId);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!allSectionsValid || !selectedStudentId) return;

    setErrorMessage(null);
    setIsSubmitting(true);

    try {
      const command: CreateSingleManualResultCommand = {
        studentId: selectedStudentId,
        sections: exam.sections.map((sec) => {
          const inp = sectionInputs[sec.id] || { correct: 0, wrong: 0, blank: 0 };
          return {
            examSectionId: sec.id,
            correct: inp.correct,
            wrong: inp.wrong,
            blank: inp.blank,
          };
        }),
      };

      const result = await examService.createSingleManualResult(exam.id, command);
      onSuccess(result);
    } catch (err: unknown) {
      setErrorMessage(
        err instanceof Error ? err.message : 'Manuel sonuç kaydedilirken bir hata oluştu.'
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  const totalCorrectAll = exam.sections.reduce((acc, sec) => acc + (sectionInputs[sec.id]?.correct || 0), 0);
  const totalWrongAll = exam.sections.reduce((acc, sec) => acc + (sectionInputs[sec.id]?.wrong || 0), 0);
  const totalBlankAll = exam.sections.reduce((acc, sec) => acc + (sectionInputs[sec.id]?.blank || 0), 0);
  const totalNetAll = exam.sections.reduce((acc, sec) => acc + calculateNet(sec, sectionInputs[sec.id] || { correct: 0, wrong: 0, blank: 0 }), 0);

  return (
    <form onSubmit={handleSubmit} className="space-y-6 max-w-4xl mx-auto">
      <div className="flex items-center justify-between border-b pb-4 dark:border-gray-800">
        <div>
          <h2 className="text-xl font-bold text-gray-900 dark:text-white">Tekil Öğrenci Manuel Sonuç Girişi</h2>
          <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
            {exam.title} ({exam.code}) — {exam.examTypeName}
          </p>
        </div>
        <Button type="button" variant="outline" size="sm" onClick={onCancel}>
          Vazgeç
        </Button>
      </div>

      {/* Retake & Multi-attempt Disclaimer */}
      <div className="p-4 rounded-xl bg-blue-50 dark:bg-blue-950/40 border border-blue-200 dark:border-blue-900 text-blue-800 dark:text-blue-300 text-xs flex items-start gap-3">
        <Info className="h-5 w-5 shrink-0 text-blue-500 mt-0.5" />
        <div>
          <div className="font-semibold text-sm">Deneme Kayıt Notu</div>
          <div>Öğrenci için önceden girilmiş sonuç varsa, bu işlem öğrenci için yeni bir sınav denemesi (retake attempt) oluşturacaktır.</div>
        </div>
      </div>

      {errorMessage && (
        <div className="p-4 rounded-xl bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-900 text-red-700 dark:text-red-300 text-xs flex items-center gap-2">
          <AlertCircle className="h-4 w-4 shrink-0" />
          <span>{errorMessage}</span>
        </div>
      )}

      {/* Öğrenci Seçimi */}
      <Card className="p-6 space-y-4">
        <label htmlFor="single-student-select" className="block text-sm font-semibold text-gray-900 dark:text-white flex items-center gap-2">
          <User className="h-4 w-4 text-navy-600" />
          Öğrenci Seçin <span className="text-red-500">*</span>
        </label>

        {loadingStudents ? (
          <div className="text-xs text-gray-500">Öğrenciler yükleniyor...</div>
        ) : (
          <select
            id="single-student-select"
            value={selectedStudentId}
            onChange={(e) => setSelectedStudentId(e.target.value)}
            required
            className="w-full rounded-xl border border-gray-300 dark:border-gray-700 bg-white dark:bg-navy-950 px-4 py-2.5 text-sm text-gray-900 dark:text-white focus:ring-2 focus:ring-navy-500 focus:outline-none"
          >
            <option value="">-- Lütfen Öğrenci Seçiniz --</option>
            {students.map((st) => (
              <option key={st.id} value={st.id}>
                {st.fullName} ({st.studentNumber || 'No yok'})
              </option>
            ))}
          </select>
        )}
      </Card>

      {/* Ders ve Soru Skoru Giriş Tablosu */}
      <Card className="overflow-hidden border border-gray-200 dark:border-gray-800">
        <div className="p-4 bg-gray-50 dark:bg-navy-900 border-b border-gray-200 dark:border-gray-800 flex items-center justify-between">
          <span className="text-sm font-semibold text-gray-900 dark:text-white">Ders Bazlı Sonuçlar</span>
          <span className="text-xs text-gray-500">Formül: Doğru - (Yanlış / {exam.wrongAnswerPenalty || 4})</span>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-sm text-left">
            <thead className="text-xs uppercase bg-gray-100 dark:bg-navy-950 text-gray-600 dark:text-gray-400 border-b border-gray-200 dark:border-gray-800">
              <tr>
                <th className="px-4 py-3">Ders</th>
                <th className="px-4 py-3 text-center">Soru</th>
                <th className="px-4 py-3 text-center w-28">Doğru</th>
                <th className="px-4 py-3 text-center w-28">Yanlış</th>
                <th className="px-4 py-3 text-center w-28">Boş</th>
                <th className="px-4 py-3 text-center">Tahmini Net</th>
                <th className="px-4 py-3 text-center">Durum</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 dark:divide-gray-800">
              {exam.sections.map((sec) => {
                const inp = sectionInputs[sec.id] || { correct: 0, wrong: 0, blank: sec.questionCount };
                const totalEntered = inp.correct + inp.wrong + inp.blank;
                const isValid = totalEntered === sec.questionCount;
                const net = calculateNet(sec, inp);

                return (
                  <tr key={sec.id} className="hover:bg-gray-50 dark:hover:bg-navy-900/50">
                    <td className="px-4 py-3 font-medium text-gray-900 dark:text-white">
                      {sec.subjectName}
                    </td>
                    <td className="px-4 py-3 text-center font-semibold text-gray-500">
                      {sec.questionCount}
                    </td>
                    <td className="px-2 py-2">
                      <Input
                        type="number"
                        min={0}
                        max={sec.questionCount}
                        value={inp.correct}
                        onChange={(e) => handleScoreChange(sec.id, 'correct', parseInt(e.target.value, 10))}
                        className="text-center font-medium"
                      />
                    </td>
                    <td className="px-2 py-2">
                      <Input
                        type="number"
                        min={0}
                        max={sec.questionCount}
                        value={inp.wrong}
                        onChange={(e) => handleScoreChange(sec.id, 'wrong', parseInt(e.target.value, 10))}
                        className="text-center font-medium"
                      />
                    </td>
                    <td className="px-2 py-2">
                      <Input
                        type="number"
                        min={0}
                        max={sec.questionCount}
                        value={inp.blank}
                        onChange={(e) => handleScoreChange(sec.id, 'blank', parseInt(e.target.value, 10))}
                        className="text-center font-medium"
                      />
                    </td>
                    <td className="px-4 py-3 text-center font-bold text-navy-600 dark:text-navy-400">
                      {net.toFixed(2)}
                    </td>
                    <td className="px-4 py-3 text-center">
                      {isValid ? (
                        <span className="inline-flex items-center gap-1 text-xs text-emerald-600 dark:text-emerald-400 font-medium">
                          <CheckCircle2 className="h-4 w-4" /> {totalEntered}/{sec.questionCount}
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 text-xs text-red-600 dark:text-red-400 font-medium" title={`Toplam ${sec.questionCount} soru olmalıdır.`}>
                          <AlertCircle className="h-4 w-4" /> {totalEntered}/{sec.questionCount}
                        </span>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
            <tfoot className="bg-gray-50 dark:bg-navy-950 font-bold border-t border-gray-200 dark:border-gray-800">
              <tr>
                <td className="px-4 py-3">TOPLAM</td>
                <td className="px-4 py-3 text-center">{exam.sections.reduce((acc, s) => acc + s.questionCount, 0)}</td>
                <td className="px-4 py-3 text-center text-emerald-600">{totalCorrectAll}</td>
                <td className="px-4 py-3 text-center text-red-600">{totalWrongAll}</td>
                <td className="px-4 py-3 text-center text-gray-500">{totalBlankAll}</td>
                <td className="px-4 py-3 text-center text-navy-600 dark:text-navy-400">{totalNetAll.toFixed(2)}</td>
                <td className="px-4 py-3 text-center">
                  <Calculator className="h-4 w-4 mx-auto text-gray-400" />
                </td>
              </tr>
            </tfoot>
          </table>
        </div>
      </Card>

      {/* Action Buttons */}
      <div className="flex items-center justify-end gap-3 pt-4">
        <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
          Vazgeç
        </Button>
        <Button
          type="submit"
          variant="primary"
          disabled={!allSectionsValid || !selectedStudentId || isSubmitting}
          isLoading={isSubmitting}
        >
          Sonucu Kaydet
        </Button>
      </div>
    </form>
  );
};
