export const translateIssueCode = (code: string, fallbackMessage?: string): string => {
  switch (code) {
    case 'STUDENT_NOT_FOUND':
      return 'Öğrenci sistemde bulunamadı';
    case 'STUDENT_DUPLICATE':
      return 'Aynı öğrenci birden fazla satırda yer alıyor';
    case 'STUDENT_IDENTIFIER_AMBIGUOUS':
      return 'Öğrenci numarası birden fazla kayıtla eşleşiyor';
    case 'STUDENT_IDENTIFIER_CONFLICT':
      return 'Öğrenci kimlik bilgileri çelişiyor';
    case 'STUDENT_MATCH_SUGGESTION':
      return 'Öğrenci eşleştirme önerisi mevcut';
    case 'DUPLICATE_ROW':
      return 'Mükerrer satır tespiti yapıldı';
    case 'INVALID_CORRECT_COUNT':
      return 'Geçersiz doğru sayısı (negatif değer girilemez)';
    case 'INVALID_WRONG_COUNT':
      return 'Geçersiz yanlış sayısı (negatif değer girilemez)';
    case 'INVALID_BLANK_COUNT':
      return 'Geçersiz boş sayısı (negatif değer girilemez)';
    case 'SECTION_NOT_FOUND':
      return 'Sınav ders bölümü bulunamadı';
    case 'QUESTION_NUMBER_NOT_FOUND':
      return 'Soru numarası bulunamadı';
    case 'INVALID_OPTION':
      return 'Geçersiz cevap seçeneği (Yalnızca A-E seçenekleri geçerlidir)';
    case 'DUPLICATE_QUESTION_COLUMN':
      return 'Mükerrer soru sütunu tespiti yapıldı';
    case 'INVALID_TEMPLATE':
      return 'Geçersiz Excel şablon yapısı';
    case 'EXAM_TEMPLATE_MISMATCH':
      return 'Bu Excel dosyası farklı bir sınav için oluşturulmuş';
    case 'UNSUPPORTED_TEMPLATE_VERSION':
      return 'Bu Excel şablon sürümü artık desteklenmiyor';
    case 'TENANT_MISMATCH':
      return 'Kurum eşleşme hatası';
    case 'FILE_CORRUPT':
      return 'Excel dosyası bozuk veya okunamıyor';
    case 'FILE_TOO_LARGE':
      return 'Dosya boyutu sınırını aşıyor';
    case 'FILE_EMPTY':
      return 'Excel dosyası boş';
    case 'INVALID_FILE_TYPE':
      return 'Yalnızca .xlsx formatındaki dosyalar desteklenir';
    case 'QUESTION_COUNT_MISMATCH':
      return 'Doğru, yanlış ve boş sayıları toplamı soru sayısıyla eşleşmiyor';
    case 'IMPORT_FILE_CHANGED':
      return 'Dosya önizlemeden sonra değişmiş. Lütfen dosyayı yeniden yükleyip tekrar kontrol edin.';
    case 'IMPORT_ALREADY_COMPLETED':
      return 'Bu Excel dosyası bu sınav için daha önce içe aktarılmış.';
    case 'IMPORT_VALIDATION_FAILED':
      return 'İçe aktarma doğrulaması başarısız oldu.';
    default:
      return fallbackMessage || code;
  }
};
