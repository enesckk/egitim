import React, { useState } from 'react';
import {
  ChevronDown,
  ChevronRight,
  BookOpen,
  FolderTree,
  FileCode,
  Layers,
} from 'lucide-react';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import {
  StudentKnowledgeMapDto,
  KnowledgeMapSubjectNodeDto,
  KnowledgeMapTopicNodeDto,
  KnowledgeMapSubTopicNodeDto,
  KnowledgeMapLearningOutcomeNodeDto,
  formatRate,
  getClassificationLabel,
  getClassificationBadgeVariant,
} from '../types';

interface AcademicKnowledgeMapTabProps {
  knowledgeMap: StudentKnowledgeMapDto | null;
}

export const AcademicKnowledgeMapTab: React.FC<AcademicKnowledgeMapTabProps> = ({
  knowledgeMap,
}) => {
  const [expandedSubjects, setExpandedSubjects] = useState<Record<string, boolean>>({});
  const [expandedTopics, setExpandedTopics] = useState<Record<string, boolean>>({});

  if (!knowledgeMap || knowledgeMap.subjects.length === 0) {
    return (
      <Card className="p-8 text-center bg-white border border-neutral-100">
        <FolderTree className="h-10 w-10 text-neutral-300 mx-auto mb-3" />
        <h3 className="text-sm font-semibold text-neutral-900">Müfredat Haritası Verisi Bulunamadı</h3>
        <p className="text-xs text-neutral-500 mt-1 max-w-sm mx-auto">
          Sınav sonuçlarınız işlendikçe ders, konu ve kazanım bazlı müfredat haritanız burada görüntülenecektir.
        </p>
      </Card>
    );
  }

  const toggleSubject = (id: string) => {
    setExpandedSubjects((prev) => ({ ...prev, [id]: !prev[id] }));
  };

  const toggleTopic = (id: string) => {
    setExpandedTopics((prev) => ({ ...prev, [id]: !prev[id] }));
  };

  return (
    <div className="space-y-4">
      {/* Knowledge Map Header Summary */}
      <div className="flex items-center justify-between bg-white p-3.5 sm:p-4 rounded-xl border border-neutral-100 text-xs">
        <div className="flex items-center gap-2">
          <FolderTree className="h-4 w-4 text-primary-600" />
          <span className="font-semibold text-neutral-900">Müfredat Ağacı Kapsamı</span>
        </div>
        <div className="flex items-center gap-3 text-neutral-500">
          <span>{knowledgeMap.summary.totalSubjects} Ders</span>
          <span>•</span>
          <span>{knowledgeMap.summary.totalTopics} Konu</span>
          <span>•</span>
          <span>{knowledgeMap.summary.totalLearningOutcomes} Kazanım</span>
        </div>
      </div>

      {/* Curriculum Tree */}
      <div className="space-y-3">
        {knowledgeMap.subjects.map((subject: KnowledgeMapSubjectNodeDto) => {
          const isSubjectExpanded = !!expandedSubjects[subject.id];

          return (
            <div
              key={subject.id}
              className="bg-white rounded-2xl border border-neutral-100 overflow-hidden shadow-soft-sm"
            >
              {/* Level 1: Subject Node Header */}
              <div
                onClick={() => toggleSubject(subject.id)}
                className="flex items-center justify-between p-4 cursor-pointer hover:bg-neutral-50/80 transition-colors"
              >
                <div className="flex items-center gap-3">
                  <button type="button" className="text-neutral-400">
                    {isSubjectExpanded ? (
                      <ChevronDown className="h-5 w-5" />
                    ) : (
                      <ChevronRight className="h-5 w-5" />
                    )}
                  </button>
                  <BookOpen className="h-5 w-5 text-primary-600" />
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="font-bold text-neutral-900 text-base">{subject.name}</span>
                      <span className="text-xs text-neutral-400 font-mono">({subject.code})</span>
                      {subject.isOrphaned && (
                        <span className="text-[10px] bg-neutral-100 text-neutral-600 px-2 py-0.5 rounded font-medium">
                          Geçmiş kayıt
                        </span>
                      )}
                    </div>
                    <p className="text-xs text-neutral-500 mt-0.5">
                      {subject.topics.length} Konu • {subject.totalQuestions} Soru
                    </p>
                  </div>
                </div>

                <div className="flex items-center gap-3">
                  {subject.hasEvidence ? (
                    <>
                      <div className="hidden sm:block text-right text-xs">
                        <span className="text-neutral-400 block">Başarı Oranı</span>
                        <span className="font-semibold text-neutral-900">
                          {formatRate(subject.successRate)}
                        </span>
                      </div>
                      <Badge variant={getClassificationBadgeVariant(subject.state)}>
                        {getClassificationLabel(subject.state)}
                      </Badge>
                    </>
                  ) : (
                    <Badge variant="secondary">Henüz ölçülmedi</Badge>
                  )}
                </div>
              </div>

              {/* Level 2: Topics Container */}
              {isSubjectExpanded && (
                <div className="border-t border-neutral-100 bg-neutral-50/40 p-3 sm:p-4 space-y-2.5">
                  {subject.topics.map((topic: KnowledgeMapTopicNodeDto) => {
                    const isTopicExpanded = !!expandedTopics[topic.id];

                    return (
                      <div
                        key={topic.id}
                        className="bg-white rounded-xl border border-neutral-200/60 overflow-hidden"
                      >
                        {/* Topic Header */}
                        <div
                          onClick={() => toggleTopic(topic.id)}
                          className="flex items-center justify-between p-3 cursor-pointer hover:bg-neutral-50 transition-colors"
                        >
                          <div className="flex items-center gap-2.5">
                            <button type="button" className="text-neutral-400">
                              {isTopicExpanded ? (
                                <ChevronDown className="h-4 w-4" />
                              ) : (
                                <ChevronRight className="h-4 w-4" />
                              )}
                            </button>
                            <Layers className="h-4 w-4 text-indigo-600" />
                            <div>
                              <span className="font-semibold text-neutral-900 text-sm">
                                {topic.name}
                              </span>
                              <span className="text-xs text-neutral-400 font-mono ml-1.5">
                                ({topic.code})
                              </span>
                            </div>
                          </div>

                          <div className="flex items-center gap-2">
                            {topic.hasEvidence ? (
                              <Badge variant={getClassificationBadgeVariant(topic.state)}>
                                {getClassificationLabel(topic.state)}
                              </Badge>
                            ) : (
                              <Badge variant="secondary">Henüz ölçülmedi</Badge>
                            )}
                          </div>
                        </div>

                        {/* Level 3 & 4: SubTopics & LearningOutcomes */}
                        {isTopicExpanded && (
                          <div className="border-t border-neutral-100 bg-neutral-50/80 p-3 space-y-3">
                            {topic.subTopics.map((subTopic: KnowledgeMapSubTopicNodeDto) => (
                              <div key={subTopic.id} className="space-y-1.5">
                                <div className="flex items-center gap-1.5 text-xs font-semibold text-neutral-700">
                                  <span className="w-1.5 h-1.5 rounded-full bg-neutral-400" />
                                  <span>{subTopic.name}</span>
                                </div>

                                <div className="pl-4 space-y-1">
                                  {subTopic.learningOutcomes.map(
                                    (lo: KnowledgeMapLearningOutcomeNodeDto) => (
                                      <div
                                        key={lo.id}
                                        className="flex items-center justify-between bg-white p-2.5 rounded-lg border border-neutral-100 text-xs"
                                      >
                                        <div className="flex items-center gap-2">
                                          <FileCode className="h-3.5 w-3.5 text-neutral-400" />
                                          <span className="font-mono text-neutral-500 text-[11px]">
                                            {lo.code}
                                          </span>
                                          <span className="font-medium text-neutral-800">
                                            {lo.name}
                                          </span>
                                        </div>

                                        <div className="flex items-center gap-2">
                                          {lo.hasEvidence ? (
                                            <>
                                              <span className="text-neutral-500 text-[11px]">
                                                {formatRate(lo.successRate)}
                                              </span>
                                              <Badge
                                                variant={getClassificationBadgeVariant(lo.state)}
                                                className="text-[10px] py-0 px-2"
                                              >
                                                {getClassificationLabel(lo.state)}
                                              </Badge>
                                            </>
                                          ) : (
                                            <span className="text-neutral-400 italic text-[11px]">
                                              Henüz ölçülmedi
                                            </span>
                                          )}
                                        </div>
                                      </div>
                                    )
                                  )}
                                </div>
                              </div>
                            ))}
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
};
