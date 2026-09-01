import React from 'react';
import { User, Mail, Calendar } from 'lucide-react';
import { StudentProfileData } from '../types';

export interface ProfileIdentityCardProps {
  profile: StudentProfileData;
}

export const ProfileIdentityCard: React.FC<ProfileIdentityCardProps> = ({ profile }) => {
  return (
    <div className="space-y-3.5 sm:space-y-4 select-none">
      {/* 1. Student Identity Card (Figma Profile.tsx) */}
      <div className="bg-white rounded-2xl border border-neutral-100 p-5 sm:p-6 flex items-center gap-4 sm:gap-5 shadow-soft-sm">
        <div className="w-14 h-14 sm:w-16 sm:h-16 rounded-2xl bg-navy-900 text-white text-lg sm:text-xl font-semibold flex items-center justify-center flex-shrink-0">
          {profile.initials}
        </div>
        <div className="flex-1 min-w-0">
          <h2 className="font-semibold text-base sm:text-lg text-neutral-900 truncate">
            {profile.name}
          </h2>
          <p className="text-neutral-500 text-xs sm:text-sm truncate">
            {profile.subtitle}
          </p>
          <span className="inline-block mt-1.5 text-xs font-medium text-primary-700 bg-primary-50 px-2.5 py-0.5 rounded-full">
            Öğrenci
          </span>
        </div>
      </div>

      {/* 2. Account Info Section (Figma Profile.tsx) */}
      <div className="bg-white rounded-2xl border border-neutral-100 overflow-hidden shadow-soft-sm">
        <div className="px-5 py-3 border-b border-neutral-100 bg-neutral-50">
          <p className="text-xs font-semibold text-neutral-500 uppercase tracking-wide">
            Hesap Bilgileri
          </p>
        </div>
        <div className="divide-y divide-neutral-50">
          <div className="flex items-center gap-3 px-5 py-3.5">
            <User className="h-4 w-4 text-neutral-400 flex-shrink-0" />
            <div className="flex-1 min-w-0">
              <p className="text-[11px] text-neutral-400">Ad Soyad</p>
              <p className="text-sm font-medium text-neutral-800 truncate">{profile.name}</p>
            </div>
          </div>

          <div className="flex items-center gap-3 px-5 py-3.5">
            <Mail className="h-4 w-4 text-neutral-400 flex-shrink-0" />
            <div className="flex-1 min-w-0">
              <p className="text-[11px] text-neutral-400">E-posta</p>
              <p className="text-sm font-medium text-neutral-800 truncate">{profile.email}</p>
            </div>
          </div>

          <div className="flex items-center gap-3 px-5 py-3.5">
            <Calendar className="h-4 w-4 text-neutral-400 flex-shrink-0" />
            <div className="flex-1 min-w-0">
              <p className="text-[11px] text-neutral-400">Platforma Katılım</p>
              <p className="text-sm font-medium text-neutral-800">{profile.joined}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
