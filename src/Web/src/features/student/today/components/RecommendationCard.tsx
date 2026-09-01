import React from 'react';
import { Info } from 'lucide-react';
import { RecommendationItem } from '../types';

export interface RecommendationCardProps {
  recommendation: RecommendationItem;
  onAction?: () => void;
}

export const RecommendationCard: React.FC<RecommendationCardProps> = ({
  recommendation,
  onAction,
}) => {
  return (
    <div className="bg-attention-light border border-purple-200 rounded-2xl p-4">
      <div className="flex items-start gap-3">
        <div className="w-8 h-8 rounded-lg bg-attention flex items-center justify-center flex-shrink-0 mt-0.5 text-white">
          <Info className="h-4 w-4" />
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-semibold text-attention-dark mb-1">Öneri</p>
          <p className="text-xs text-purple-700 leading-relaxed">
            {recommendation.message}
          </p>
          <button
            type="button"
            onClick={onAction}
            className="mt-2 text-xs text-attention font-medium hover:underline transition-all block focus:outline-none focus:ring-1 focus:ring-attention rounded"
          >
            {recommendation.actionLabel}
          </button>
        </div>
      </div>
    </div>
  );
};
