export type AcademicClassificationState =
  | 'Critical'
  | 'Weak'
  | 'Developing'
  | 'Strong'
  | 'InsufficientData';

export type TrendDirection = 'Increasing' | 'Stable' | 'Decreasing';

export type ClassificationBasis = 'RecentWindow' | 'OverallHistory';

export type AcademicRecommendationType =
  | 'RemediateCritical'
  | 'StrengthenWeak'
  | 'ReinforceDeveloping'
  | 'MonitorDeclining';

export type RecommendationNodeType = 'Subject' | 'Topic' | 'LearningOutcome';

export interface StudentSubjectPerformanceDto {
  subjectId: string;
  subjectName: string;
  examCount: number;
  attemptCount: number;
  totalQuestions: number;
  attemptedCount: number;
  totalCorrect: number;
  totalWrong: number;
  totalBlank: number;
  totalNet: number;
  accuracy: number | null;
  successRate: number | null;
  coverage: number | null;
  rawAttemptCount: number;
  aggregateAttemptCount: number;
}

export interface StudentTopicPerformanceDto {
  topicId: string;
  topicName: string;
  subjectId: string;
  subjectName: string;
  examCount: number;
  attemptCount: number;
  totalQuestions: number;
  attemptedCount: number;
  totalCorrect: number;
  totalWrong: number;
  totalBlank: number;
  totalNet: number;
  accuracy: number | null;
  successRate: number | null;
  coverage: number | null;
}

export interface StudentLearningOutcomePerformanceDto {
  learningOutcomeId: string;
  learningOutcomeCode: string;
  learningOutcomeTitle: string;
  subTopicId: string | null;
  subTopicName: string | null;
  topicId: string;
  topicName: string;
  subjectId: string;
  subjectName: string;
  examCount: number;
  totalQuestions: number;
  attemptedCount: number;
  totalCorrect: number;
  totalWrong: number;
  totalBlank: number;
  totalNet: number;
  accuracy: number | null;
  successRate: number | null;
  coverage: number | null;
}

export interface AcademicCoverageSummaryDto {
  studentId: string;
  totalFinalizedAttempts: number;
  rawAttemptsCount: number;
  aggregateAttemptsCount: number;
  rawCoveragePercentage: number;
  totalQuestionsEvaluated: number;
  taggedQuestionsEvaluated: number;
  learningOutcomeTagCoveragePercentage: number;
}

export interface TrendWindowMetricsDto {
  observationCount: number;
  totalQuestions: number;
  correct: number;
  wrong: number;
  blank: number;
  totalNet: number;
  netPerQuestion: number;
  accuracy: number;
  successRate: number;
  coverage: number;
}

export interface SubjectObservationPointDto {
  attemptId: string;
  examId: string;
  examTitle: string;
  takenAt: string;
  attemptNumber: number;
  subjectId: string;
  totalQuestions: number;
  correct: number;
  wrong: number;
  blank: number;
  net: number;
  netPerQuestion: number;
  accuracy: number;
  successRate: number;
  coverage: number;
}

export interface TopicObservationPointDto {
  attemptId: string;
  examId: string;
  examTitle: string;
  takenAt: string;
  attemptNumber: number;
  topicId: string;
  totalQuestions: number;
  correct: number;
  wrong: number;
  blank: number;
  net: number;
  netPerQuestion: number;
  accuracy: number;
  successRate: number;
  coverage: number;
}

export interface LearningOutcomeObservationPointDto {
  attemptId: string;
  examId: string;
  examTitle: string;
  takenAt: string;
  attemptNumber: number;
  learningOutcomeId: string;
  totalQuestions: number;
  correct: number;
  wrong: number;
  blank: number;
  accuracy: number;
  successRate: number;
  coverage: number;
}

export interface SubjectTrendDto {
  subjectId: string;
  subjectName: string;
  totalObservations: number;
  windowSize: number;
  hasEnoughData: boolean;
  previousWindow: TrendWindowMetricsDto | null;
  recentWindow: TrendWindowMetricsDto | null;
  successRateDelta: number | null;
  accuracyDelta: number | null;
  coverageDelta: number | null;
  netPerQuestionDelta: number | null;
  direction: TrendDirection | null;
  timeSeries: SubjectObservationPointDto[];
}

export interface TopicTrendDto {
  topicId: string;
  topicName: string;
  subjectId: string;
  subjectName: string;
  totalObservations: number;
  windowSize: number;
  hasEnoughData: boolean;
  previousWindow: TrendWindowMetricsDto | null;
  recentWindow: TrendWindowMetricsDto | null;
  successRateDelta: number | null;
  accuracyDelta: number | null;
  coverageDelta: number | null;
  netPerQuestionDelta: number | null;
  direction: TrendDirection | null;
  timeSeries: TopicObservationPointDto[];
}

export interface LearningOutcomeTrendDto {
  learningOutcomeId: string;
  learningOutcomeCode: string;
  learningOutcomeTitle: string;
  topicId: string;
  topicName: string;
  subjectId: string;
  subjectName: string;
  totalObservations: number;
  windowSize: number;
  hasEnoughData: boolean;
  previousWindow: TrendWindowMetricsDto | null;
  recentWindow: TrendWindowMetricsDto | null;
  successRateDelta: number | null;
  accuracyDelta: number | null;
  coverageDelta: number | null;
  direction: TrendDirection | null;
  timeSeries: LearningOutcomeObservationPointDto[];
}

export interface SubjectClassificationDto {
  subjectId: string;
  subjectName: string;
  state: AcademicClassificationState;
  hasEnoughData: boolean;
  basis: ClassificationBasis;
  observationCount: number;
  totalQuestions: number;
  attemptedCount: number;
  totalCorrect: number;
  totalWrong: number;
  totalBlank: number;
  successRate: number | null;
  accuracy: number | null;
  coverage: number | null;
  trendDirection: TrendDirection | null;
  successRateDelta: number | null;
  policyVersion: string;
  insufficiencyReason: string;
}

export interface TopicClassificationDto {
  topicId: string;
  topicName: string;
  subjectId: string;
  subjectName: string;
  state: AcademicClassificationState;
  hasEnoughData: boolean;
  basis: ClassificationBasis;
  observationCount: number;
  totalQuestions: number;
  attemptedCount: number;
  totalCorrect: number;
  totalWrong: number;
  totalBlank: number;
  successRate: number | null;
  accuracy: number | null;
  coverage: number | null;
  trendDirection: TrendDirection | null;
  successRateDelta: number | null;
  policyVersion: string;
  insufficiencyReason: string;
}

export interface LearningOutcomeClassificationDto {
  learningOutcomeId: string;
  learningOutcomeCode: string;
  learningOutcomeTitle: string;
  topicId: string;
  topicName: string;
  subjectId: string;
  subjectName: string;
  state: AcademicClassificationState;
  hasEnoughData: boolean;
  basis: ClassificationBasis;
  observationCount: number;
  totalQuestions: number;
  attemptedCount: number;
  totalCorrect: number;
  totalWrong: number;
  totalBlank: number;
  successRate: number | null;
  accuracy: number | null;
  coverage: number | null;
  trendDirection: TrendDirection | null;
  successRateDelta: number | null;
  policyVersion: string;
  insufficiencyReason: string;
}

export interface KnowledgeMapLearningOutcomeNodeDto {
  id: string;
  code: string;
  name: string;
  state: AcademicClassificationState;
  hasEnoughData: boolean;
  hasEvidence: boolean;
  basis: ClassificationBasis;
  observationCount: number;
  totalQuestions: number;
  attemptedCount: number;
  totalCorrect: number;
  totalWrong: number;
  totalBlank: number;
  successRate: number | null;
  accuracy: number | null;
  coverage: number | null;
  trendDirection: TrendDirection | null;
  successRateDelta: number | null;
  isOrphaned?: boolean;
}

export interface KnowledgeMapSubTopicNodeDto {
  id: string;
  code: string;
  name: string;
  learningOutcomes: KnowledgeMapLearningOutcomeNodeDto[];
  isOrphaned?: boolean;
}

export interface KnowledgeMapTopicNodeDto {
  id: string;
  code: string;
  name: string;
  state: AcademicClassificationState;
  hasEnoughData: boolean;
  hasEvidence: boolean;
  basis: ClassificationBasis;
  observationCount: number;
  totalQuestions: number;
  attemptedCount: number;
  totalCorrect: number;
  totalWrong: number;
  totalBlank: number;
  successRate: number | null;
  accuracy: number | null;
  coverage: number | null;
  trendDirection: TrendDirection | null;
  successRateDelta: number | null;
  subTopics: KnowledgeMapSubTopicNodeDto[];
  isOrphaned?: boolean;
}

export interface KnowledgeMapSubjectNodeDto {
  id: string;
  code: string;
  name: string;
  state: AcademicClassificationState;
  hasEnoughData: boolean;
  hasEvidence: boolean;
  basis: ClassificationBasis;
  observationCount: number;
  totalQuestions: number;
  attemptedCount: number;
  totalCorrect: number;
  totalWrong: number;
  totalBlank: number;
  successRate: number | null;
  accuracy: number | null;
  coverage: number | null;
  trendDirection: TrendDirection | null;
  successRateDelta: number | null;
  topics: KnowledgeMapTopicNodeDto[];
  isOrphaned?: boolean;
}

export interface KnowledgeMapSummaryDto {
  totalSubjects: number;
  subjectsWithEvidence: number;
  strongSubjects: number;
  weakSubjects: number;
  criticalSubjects: number;
  totalTopics: number;
  topicsWithEvidence: number;
  totalLearningOutcomes: number;
  learningOutcomesWithEvidence: number;
}

export interface StudentKnowledgeMapDto {
  studentId: string;
  policyVersion: string;
  examTypeId: string | null;
  summary: KnowledgeMapSummaryDto;
  subjects: KnowledgeMapSubjectNodeDto[];
}

export interface AcademicRecommendationDto {
  recommendationType: AcademicRecommendationType;
  nodeType: RecommendationNodeType;
  nodeId: string;
  code: string;
  name: string;
  subjectId: string | null;
  subjectName: string | null;
  topicId: string | null;
  topicName: string | null;
  subTopicId: string | null;
  subTopicName: string | null;
  learningOutcomeId: string | null;
  learningOutcomeName: string | null;
  classificationState: AcademicClassificationState;
  classificationBasis: ClassificationBasis;
  successRate: number | null;
  accuracy: number | null;
  coverage: number | null;
  trendDirection: TrendDirection | null;
  successRateDelta: number | null;
  observationCount: number;
  totalQuestions: number;
  priorityScore: number;
  classificationPolicyVersion: string;
  recommendationPolicyVersion: string;
  isOrphaned?: boolean;
}

export interface StudyPlanSessionDto {
  dayIndex: number;
  date: string | null;
  sequence: number;
  durationMinutes: number;
  recommendationType: AcademicRecommendationType;
  nodeType: RecommendationNodeType;
  nodeId: string;
  code: string;
  name: string;
  subjectId: string | null;
  subjectName: string | null;
  topicId: string | null;
  topicName: string | null;
  subTopicId: string | null;
  subTopicName: string | null;
  learningOutcomeId: string | null;
  learningOutcomeName: string | null;
  classificationState: AcademicClassificationState;
  priorityScore: number;
  isOrphaned?: boolean;
}

export interface StudyPlanDayDto {
  dayIndex: number;
  date: string | null;
  plannedMinutes: number;
  sessionCount: number;
  sessions: StudyPlanSessionDto[];
}

export interface AcademicStudyPlanDto {
  studentId: string;
  planDays: number;
  dailyStudyMinutes: number;
  sessionMinutes: number;
  totalAvailableMinutes: number;
  totalPlannedMinutes: number;
  unallocatedMinutes: number;
  utilizationRate: number;
  scheduledRecommendationCount: number;
  unscheduledRecommendationCount: number;
  recommendationPolicyVersion: string;
  studyPlanPolicyVersion: string;
  days: StudyPlanDayDto[];
  unscheduledRecommendations: AcademicRecommendationDto[];
}

export type PersistentStudyPlanStatus = 'Draft' | 'Active' | 'Completed' | 'Cancelled';
export type PersistentStudyPlanSource = 'AcademicIntelligence';

export interface PersistentStudyPlanDto {
  id: string;
  institutionId: string;
  studentId: string;
  title: string;
  startDate: string;
  endDate: string;
  status: PersistentStudyPlanStatus;
  source: PersistentStudyPlanSource;
  recommendationPolicyVersion: string;
  studyPlanPolicyVersion: string;
  activatedAt: string | null;
  completedAt: string | null;
  planDays: number;
  dailyStudyMinutes: number;
  sessionMinutes: number;
  totalAvailableMinutes: number;
  totalPlannedMinutes: number;
  unallocatedMinutes: number;
  utilizationRate: number;
  scheduledRecommendationCount: number;
  unscheduledRecommendationCount: number;
  completedTaskCount: number;
  totalTaskCount: number;
  remainingTaskCount: number;
  completedTaskMinutes: number;
  taskProgressRate: number;
  minuteProgressRate: number;
  createdAt: string;
}

export interface PersistentStudyTaskDto {
  id: string;
  studyPlanId: string;
  studentId: string;
  dayIndex: number;
  plannedDate: string | null;
  sequence: number;
  durationMinutes: number;
  nodeType: RecommendationNodeType;
  nodeId: string;
  subjectId: string | null;
  subjectName: string | null;
  topicId: string | null;
  topicName: string | null;
  subTopicId: string | null;
  subTopicName: string | null;
  learningOutcomeId: string | null;
  learningOutcomeName: string | null;
  titleSnapshot: string;
  classificationStateSnapshot: AcademicClassificationState;
  priorityScoreSnapshot: number;
  recommendationType: AcademicRecommendationType;
  isCompleted: boolean;
  completedAt: string | null;
}

export interface PersistentStudyPlanDetailDto {
  plan: PersistentStudyPlanDto;
  tasks: PersistentStudyTaskDto[];
}

// User-Facing Presentation Helpers
export function getClassificationLabel(state: AcademicClassificationState): string {
  switch (state) {
    case 'Critical':
      return 'Kritik';
    case 'Weak':
      return 'Geliştirilmesi Gereken';
    case 'Developing':
      return 'Gelişiyor';
    case 'Strong':
      return 'Güçlü';
    case 'InsufficientData':
    default:
      return 'Henüz yeterli veri yok';
  }
}

export function getClassificationBadgeVariant(
  state: AcademicClassificationState
): 'danger' | 'warning' | 'info' | 'success' | 'secondary' {
  switch (state) {
    case 'Critical':
      return 'danger';
    case 'Weak':
      return 'warning';
    case 'Developing':
      return 'info';
    case 'Strong':
      return 'success';
    case 'InsufficientData':
    default:
      return 'secondary';
  }
}

export function getRecommendationTypeLabel(type: AcademicRecommendationType): string {
  switch (type) {
    case 'RemediateCritical':
      return 'Öncelikli Çalışma Alanı';
    case 'StrengthenWeak':
      return 'Geliştirme Alanı';
    case 'ReinforceDeveloping':
      return 'Pekiştirme Alanı';
    case 'MonitorDeclining':
      return 'İzleme Alanı';
    default:
      return 'Öneri';
  }
}

export function getTrendDirectionLabel(trend: TrendDirection | null): string {
  if (!trend) return 'Veri Yetersiz';
  switch (trend) {
    case 'Increasing':
      return 'Yükselişte';
    case 'Stable':
      return 'Kararlı';
    case 'Decreasing':
      return 'Düşüşte';
  }
}

export function formatRate(rate: number | null | undefined): string {
  if (rate === null || rate === undefined) {
    return 'Veri yok';
  }
  return `%${Math.round(rate * 100)}`;
}

export interface AcademicAiSummaryDto {
  audience: string;
  summary: string;
  strengths: string[];
  improvementAreas: string[];
  trendNotes: string[];
  suggestedFocus: string[];
  isAvailable: boolean;
  generatedAt: string;
}
