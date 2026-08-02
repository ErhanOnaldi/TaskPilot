import type {
  Comment,
  KnowledgeFolder,
  Note,
  Notification,
  Project,
  TaskItem,
  Workspace,
} from '../types/domain';

const iso = (day: number, hour = 9) => `2026-08-${String(day).padStart(2, '0')}T${String(hour).padStart(2, '0')}:00:00+03:00`;

export const demoWorkspace: Workspace = {
  id: 1,
  name: 'Northstar Studio',
  isArchived: false,
  createdAt: '2026-01-12T09:00:00+03:00',
  updatedAt: iso(2),
};

export const demoProjects: Project[] = [
  { id: 1, workspaceId: 1, name: 'Atlas Web Platform', description: 'Müşteriye dönük web platformunun yeniden yapılandırılması.', status: 'Active', createdAt: iso(1), updatedAt: iso(2, 11) },
  { id: 2, workspaceId: 1, name: 'Mobile Checkout', description: 'Mobil ödeme akışının dönüşüm odaklı iyileştirilmesi.', status: 'Active', createdAt: iso(1), updatedAt: iso(1, 17) },
  { id: 3, workspaceId: 1, name: 'Internal Developer Portal', description: 'Servis kataloğu, runbook ve dağıtım self-servisi.', status: 'Active', createdAt: iso(1), updatedAt: '2026-07-30T13:00:00+03:00' },
  { id: 4, workspaceId: 1, name: 'Legacy Billing Migration', description: 'Eski faturalama sisteminin devri tamamlandı.', status: 'Completed', createdAt: iso(1), updatedAt: '2026-06-12T13:00:00+03:00' },
  { id: 5, workspaceId: 1, name: 'Design System v1', description: 'İlk sürüm bileşen kütüphanesi; arşivlendi.', status: 'Archived', createdAt: iso(1), updatedAt: '2026-05-02T13:00:00+03:00' },
];

export const demoTasks: TaskItem[] = [
  { id: 142, projectId: 1, title: 'Password recovery e-posta teslim hatalarını araştır', description: 'Sağlayıcı bounce loglarını, suppression listesini ve retry politikasını incele.', status: 'InProgress', priority: 'Critical', dueDate: iso(5), assignedUserId: 3, createdByUserId: 1, createdAt: '2026-07-24T14:02:00+03:00', updatedAt: iso(2, 10), completedAt: null },
  { id: 138, projectId: 1, title: 'Fatura dışa aktarım performansını iyileştir', description: '10k satır üzerindeki CSV dışa aktarımında streaming üretimini doğrula.', status: 'InReview', priority: 'High', dueDate: iso(7), assignedUserId: 4, createdByUserId: 1, createdAt: '2026-07-20T11:00:00+03:00', updatedAt: iso(2, 7), completedAt: null },
  { id: 151, projectId: 1, title: 'Authentication uçlarına rate limiting ekle', description: 'Login ve refresh uçlarına IP ve hesap bazlı kademeli limit uygula.', status: 'Todo', priority: 'High', dueDate: iso(12), assignedUserId: null, createdByUserId: 2, createdAt: '2026-07-31T10:00:00+03:00', updatedAt: iso(1), completedAt: null },
  { id: 129, projectId: 1, title: 'Incident response akışını dokümante et', description: 'Sev1 olaylarda ilk 15 dakika ve rol dağılımı.', status: 'Done', priority: 'Medium', dueDate: '2026-07-24T09:00:00+03:00', assignedUserId: 2, createdByUserId: 1, createdAt: '2026-07-10T09:00:00+03:00', updatedAt: '2026-07-30T11:18:00+03:00', completedAt: '2026-07-30T11:18:00+03:00' },
  { id: 147, projectId: 1, title: 'Dashboard cache invalidation akışını optimize et', description: 'Görev güncellemesi sonrası sayaçların bayat kalmasını önle.', status: 'InProgress', priority: 'Medium', dueDate: iso(9), assignedUserId: 5, createdByUserId: 1, createdAt: '2026-07-28T09:00:00+03:00', updatedAt: iso(1), completedAt: null },
  { id: 155, projectId: 1, title: 'Refresh token rotasyonunu denetle', description: 'Tüm istemcilerde tek kullanımlık refresh token davranışını doğrula.', status: 'Todo', priority: 'Critical', dueDate: iso(4), assignedUserId: 1, createdByUserId: 1, createdAt: '2026-08-01T09:00:00+03:00', updatedAt: iso(2, 8), completedAt: null },
  { id: 118, projectId: 1, title: 'Deprecated /v1 search endpointini kaldır', description: 'Harici tüketiciler nedeniyle 2027 planına taşındı.', status: 'Cancelled', priority: 'Low', dueDate: '2026-07-18T09:00:00+03:00', assignedUserId: 4, createdByUserId: 1, createdAt: '2026-07-01T09:00:00+03:00', updatedAt: '2026-07-19T09:00:00+03:00', completedAt: null },
  { id: 160, projectId: 1, title: 'Proje listesine empty state ekle', description: 'Boş proje listesinde bir sonraki mantıklı aksiyonu göster.', status: 'Todo', priority: 'Low', dueDate: iso(20), assignedUserId: null, createdByUserId: 3, createdAt: iso(2, 6), updatedAt: iso(2, 6), completedAt: null },
];

export const demoFolders: KnowledgeFolder[] = [
  { id: 1, workspaceId: 1, projectId: null, parentFolderId: null, name: 'Architecture', slug: 'architecture', updatedAt: iso(2) },
  { id: 2, workspaceId: 1, projectId: 1, parentFolderId: null, name: 'Runbooks', slug: 'runbooks', updatedAt: iso(1) },
];

export const demoNotes: Note[] = [
  { id: 1, workspaceId: 1, projectId: 1, folderId: 1, title: 'Authentication Architecture', slug: 'authentication-architecture', version: 7, updatedAt: iso(2), content: '# Authentication Architecture\n\nTaskPilot kimlik doğrulaması kısa ömürlü JWT access token ve rotasyonlu refresh token üzerine kuruludur.\n\n## Token yaşam döngüsü\n\n- Access token: 15 dakika\n- Refresh token: 30 gün, her kullanımda rotasyon\n- Logout sunucu tarafında refresh tokenı geçersiz kılar\n\n## İlgili çalışmalar\n\nParola sıfırlama tarafındaki teslim sorunları [[password-recovery-flow|Password Recovery Flow]] notunda izleniyor. Güvenlik kararlarının gerekçeleri [[api-security-decisions]] içinde tutulur.' },
  { id: 2, workspaceId: 1, projectId: null, folderId: 1, title: 'Incident Response Playbook', slug: 'incident-response-playbook', version: 3, updatedAt: '2026-07-27T09:00:00+03:00', content: '# Incident Response Playbook\n\nSev1 olaylarda ilk 15 dakika.\n\n1. Olayı ilan et\n2. Komuta rolünü ata\n3. Durum sayfasını güncelle\n\nKimlik doğrulama olayları için [[authentication-architecture]] notuna bakın.' },
  { id: 3, workspaceId: 1, projectId: 1, folderId: 2, title: 'Password Recovery Flow', slug: 'password-recovery-flow', version: 5, updatedAt: iso(2, 8), content: '# Password Recovery Flow\n\nKullanıcı e-posta girer, tek kullanımlık token üretilir ve 30 dakika geçerlidir.\n\nBu akış [[authentication-architecture|Authentication Architecture]] ve [[incident-response-playbook]] notlarıyla ilişkilidir.' },
  { id: 4, workspaceId: 1, projectId: null, folderId: 1, title: 'API Security Decisions', slug: 'api-security-decisions', version: 9, updatedAt: '2026-07-29T09:00:00+03:00', content: '# API Security Decisions\n\n## ADR-004 Rate limiting\n\nKademeli limit; 429 yanıtında Retry-After zorunlu.' },
];

export const demoComments: Comment[] = [
  { id: 1, taskId: 142, userId: 3, content: 'Sağlayıcı loglarında üç saatlik pencerede %12 soft bounce görünüyor.', createdAt: iso(2, 9), updatedAt: iso(2, 9) },
  { id: 2, taskId: 142, userId: 1, content: 'Retry politikasını exponential backoff ile üç denemeye çıkaralım.', createdAt: iso(2, 10), updatedAt: iso(2, 10) },
];

export const demoNotifications: Notification[] = [
  { id: 1, type: 'TaskAssigned', title: 'Yeni görev', message: 'TP-155 “Refresh token rotasyonunu denetle” size atandı.', isRead: false, relatedEntityId: 155, createdAt: iso(2, 11) },
  { id: 2, type: 'AiSuggestionCompleted', title: 'AI önerisi hazır', message: 'Password recovery teslim sorunları için görev önerisi hazır.', isRead: false, relatedEntityId: 7, createdAt: iso(2, 10) },
  { id: 3, type: 'CommentAdded', title: 'Yeni yorum', message: 'Tomás Ruiz TP-138 üzerinde yorum yaptı.', isRead: false, relatedEntityId: 138, createdAt: iso(2, 9) },
  { id: 4, type: 'WeeklyReportPendingReview', title: 'Rapor review bekliyor', message: '26 Tem haftalık raporu review için hazır.', isRead: true, relatedEntityId: 1, createdAt: iso(1, 9) },
];
