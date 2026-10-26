import React from 'react';
import { User, Mail, Building, Shield, Award } from 'lucide-react';
import { StudentProfileData } from '../types';

export interface ProfileIdentityCardProps {
  profile: StudentProfileData;
  institutionName?: string;
  roleLabel?: string;
}

export const ProfileIdentityCard: React.FC<ProfileIdentityCardProps> = ({
  profile,
  institutionName = 'Demo Eğitim Kurumu',
  roleLabel = 'Öğrenci',
}) => {
  return (
    <div className="space-y-4 select-none">
      {/* 1. Premium Navy Identity Banner */}
      <div className="relative overflow-hidden bg-gradient-to-br from-[#14233E] via-[#1a2e51] to-[#0f1b30] text-white rounded-3xl p-6 sm:p-7 shadow-lg border border-neutral-800/40">
        {/* Subtle decorative glow */}
        <div className="absolute -right-10 -top-10 w-44 h-44 bg-[#315FE8]/20 rounded-full blur-3xl pointer-events-none" />
        <div className="absolute -left-10 -bottom-10 w-44 h-44 bg-emerald-500/10 rounded-full blur-3xl pointer-events-none" />

        <div className="relative z-10 flex flex-col sm:flex-row items-center sm:items-start text-center sm:text-left gap-4 sm:gap-6">
          {/* Avatar Initials Badge */}
          <div className="w-18 h-18 sm:w-20 sm:h-20 rounded-2xl bg-white/10 backdrop-blur-md border border-white/20 text-white text-2xl sm:text-3xl font-extrabold flex items-center justify-center flex-shrink-0 shadow-inner">
            {profile.initials}
          </div>

          <div className="flex-1 min-w-0">
            <div className="flex flex-wrap items-center justify-center sm:justify-start gap-2 mb-1.5">
              <span className="inline-flex items-center gap-1.5 text-xs font-semibold px-3 py-1 rounded-full bg-[#315FE8]/25 text-blue-200 border border-blue-400/30">
                <Award className="h-3.5 w-3.5 text-blue-300" />
                {roleLabel}
              </span>
            </div>

            <h2 className="text-xl sm:text-2xl font-bold tracking-tight text-white truncate" data-testid="profile-full-name">
              {profile.name}
            </h2>

            <p className="text-xs sm:text-sm text-neutral-300 flex items-center justify-center sm:justify-start gap-1.5 mt-1 truncate">
              <Building className="h-3.5 w-3.5 text-neutral-400 flex-shrink-0" />
              <span>{institutionName}</span>
            </p>
          </div>
        </div>
      </div>

      {/* 2. Account Information Card */}
      <div className="bg-white rounded-2xl border border-neutral-200/70 p-5 sm:p-6 shadow-xs space-y-4">
        <div className="border-b border-neutral-100 pb-3">
          <h3 className="text-sm font-bold text-[#14233E]">Hesap Bilgileri</h3>
          <p className="text-xs text-neutral-500 mt-0.5">Sisteme kayıtlı temel kimlik ve iletişim detaylarınız</p>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5 pt-1">
          {/* Ad Soyad */}
          <div className="flex items-start gap-3 p-3.5 rounded-xl bg-[#F8F7F3]/70 border border-neutral-200/50">
            <div className="p-2 rounded-lg bg-white text-[#315FE8] shadow-xs">
              <User className="h-4 w-4" />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-[11px] font-medium text-neutral-500">Ad Soyad</p>
              <p className="text-sm font-semibold text-[#14233E] truncate">{profile.name}</p>
            </div>
          </div>

          {/* E-posta */}
          <div className="flex items-start gap-3 p-3.5 rounded-xl bg-[#F8F7F3]/70 border border-neutral-200/50">
            <div className="p-2 rounded-lg bg-white text-[#315FE8] shadow-xs">
              <Mail className="h-4 w-4" />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-[11px] font-medium text-neutral-500">E-posta Adresi</p>
              <p className="text-sm font-semibold text-[#14233E] truncate">{profile.email}</p>
            </div>
          </div>

          {/* Bağlı Kurum */}
          <div className="flex items-start gap-3 p-3.5 rounded-xl bg-[#F8F7F3]/70 border border-neutral-200/50">
            <div className="p-2 rounded-lg bg-white text-[#315FE8] shadow-xs">
              <Building className="h-4 w-4" />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-[11px] font-medium text-neutral-500">Kurum / Şube</p>
              <p className="text-sm font-semibold text-[#14233E] truncate">{institutionName}</p>
            </div>
          </div>

          {/* Rol / Yetki */}
          <div className="flex items-start gap-3 p-3.5 rounded-xl bg-[#F8F7F3]/70 border border-neutral-200/50">
            <div className="p-2 rounded-lg bg-white text-[#315FE8] shadow-xs">
              <Shield className="h-4 w-4" />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-[11px] font-medium text-neutral-500">Rol Seviyesi</p>
              <p className="text-sm font-semibold text-[#14233E] truncate">{roleLabel}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
