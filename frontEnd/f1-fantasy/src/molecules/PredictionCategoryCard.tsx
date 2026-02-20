import { RightOutlined } from "@ant-design/icons";
import { F1Card, F1Title, F1Text } from "../atoms";
import { CATEGORY_LABELS, CATEGORY_DESCRIPTIONS } from "../constants/predictionCategories";
import type { PredictionCategoryId } from "../types/predictions";

type PredictionCategoryCardProps = {
  categoryId: PredictionCategoryId;
  content: React.ReactNode;
  score?: number;
  onClick?: () => void;
};

export function PredictionCategoryCard({ categoryId, content, score, onClick }: PredictionCategoryCardProps) {
  return (
    <F1Card
      hoverable={!!onClick}
      onClick={onClick}
      className={`min-w-0 ${onClick ? "cursor-pointer !rounded-lg" : "!rounded-lg"}`}
    >
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="min-w-0 flex-1 break-words">
          <F1Title level={5} className="!mb-0.5 !text-base">
            {CATEGORY_LABELS[categoryId]}
          </F1Title>
          <F1Text muted className="text-xs block">
            {CATEGORY_DESCRIPTIONS[categoryId]}
          </F1Text>
        </div>
        <div className="flex items-center gap-2 shrink-0">
          {score !== undefined && (
            <span className="text-f1-gold font-mono text-sm font-semibold">{score} pts</span>
          )}
          {onClick && <RightOutlined className="text-f1-silver/60" />}
        </div>
      </div>
      {content && <div className="mt-3 pt-3 border-t border-f1-gray">{content}</div>}
    </F1Card>
  );
}
