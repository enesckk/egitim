import { TeacherAcademicViewModel } from './types';

export const initialTeacherAcademicData: TeacherAcademicViewModel = {
  teacherName: 'Ayşe Öğretmen',
  assignedSubjects: ['Matematik'],
  totalAssignedClasses: 1,
  totalAssignedStudents: 2,
  classrooms: [
    { id: 'cls-1', name: '12-A', branch: 'Sayısal', gradeLevel: 12, studentCount: 2 }
  ],
  gaps: [],
  topicItems: [],
};
