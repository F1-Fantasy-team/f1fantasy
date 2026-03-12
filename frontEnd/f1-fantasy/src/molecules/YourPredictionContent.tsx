import { F1Text } from "../atoms";
import { PredictionCategoryCard } from "./PredictionCategoryCard";
import { DriverAvatar } from "./DriverAvatar";
import { CATEGORY_IDS } from "../constants/predictionCategories";
import type { UserPredictions } from "../types/predictions";
import type { PredictionCategoryId } from "../types/predictions";
import type { Driver } from "../types/driver";
import type { Constructor } from "../types/constructor";
import { useDrivers, useConstructors } from "../state/useDriversAndConstructors";

export function getDriverName(id: string, drivers: Driver[]) {
  return drivers.find((d) => d.id === id)?.name ?? id;
}
export function getConstructorName(id: string, constructors: Constructor[]) {
  return constructors.find((c) => c.id === id)?.name ?? id;
}

function getCategoryScore(standing: { categoryScores: { categoryId: string; score: number }[] }, categoryId: string) {
  const entry = standing.categoryScores.find((c) => c.categoryId === categoryId);
  return entry ? entry.score : 0;
}

type PredictionContentProps = {
  predictions: UserPredictions | undefined;
  categoryId: PredictionCategoryId;
  drivers: Driver[];
  constructors: Constructor[];
};

function PredictionContent({ predictions, categoryId, drivers, constructors }: PredictionContentProps) {
  if (!predictions) {
    return <F1Text muted>No prediction yet.</F1Text>;
  }

  switch (categoryId) {
    case "driversChampionship": {
      const p = predictions.driversChampionship;
      if (!p?.length) return <F1Text muted>No prediction yet.</F1Text>;
      const sorted = [...p].sort((a, b) => a.position - b.position);
      return (
        <div className="min-w-0 overflow-x-auto [-webkit-overflow-scrolling:touch]">
          <table className="w-full min-w-[200px] text-sm">
            <thead>
              <tr className="border-b border-f1-gray text-left text-f1-silver/70">
                <th className="py-1 pr-3 font-normal">Pos</th>
                <th className="py-1 font-normal">Driver</th>
              </tr>
            </thead>
            <tbody>
              {sorted.map(({ position, driverId }) => (
                <tr key={position} className="border-b border-f1-gray/50">
                  <td className="py-1.5 pr-3 tabular-nums text-f1-silver/80">{position}</td>
                  <td className="py-1.5 text-f1-silver">{getDriverName(driverId, drivers)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      );
    }
    case "constructorsChampionship": {
      const p = predictions.constructorsChampionship;
      if (!p?.length) return <F1Text muted>No prediction yet.</F1Text>;
      const sorted = [...p].sort((a, b) => a.position - b.position);
      return (
        <div className="min-w-0 overflow-x-auto [-webkit-overflow-scrolling:touch]">
          <table className="w-full min-w-[200px] text-sm">
            <thead>
              <tr className="border-b border-f1-gray text-left text-f1-silver/70">
                <th className="py-1 pr-3 font-normal">Pos</th>
                <th className="py-1 font-normal">Constructor</th>
              </tr>
            </thead>
            <tbody>
              {sorted.map(({ position, constructorId }) => (
                <tr key={position} className="border-b border-f1-gray/50">
                  <td className="py-1.5 pr-3 text-f1-silver/80 tabular-nums">{position}</td>
                  <td className="py-1.5 text-f1-silver">{getConstructorName(constructorId, constructors)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      );
    }
    case "driverDraft": {
      const p = predictions.driverDraft;
      if (!p) return <F1Text muted>No prediction yet.</F1Text>;
      return (
        <div className="flex flex-wrap items-center gap-4">
          <DriverAvatar driverId={p.driverId1} size={56} showName />
          <span className="text-f1-silver/60">+</span>
          <DriverAvatar driverId={p.driverId2} size={56} showName />
        </div>
      );
    }
    case "destructors": {
      const p = predictions.destructors;
      if (!p) return <F1Text muted>No prediction yet.</F1Text>;
      return (
        <div className="flex flex-wrap items-center gap-4">
          <DriverAvatar driverId={p.driverId1} size={56} showName />
          <span className="text-f1-silver/60">+</span>
          <DriverAvatar driverId={p.driverId2} size={56} showName />
        </div>
      );
    }
    case "mrSaturday": {
      const p = predictions.mrSaturday;
      if (!p) return <F1Text muted>No prediction yet.</F1Text>;
      return (
        <div className="flex flex-wrap items-center gap-4">
          <DriverAvatar driverId={p.driverId1} size={56} showName />
          <span className="text-f1-silver/60">+</span>
          <DriverAvatar driverId={p.driverId2} size={56} showName />
        </div>
      );
    }
    case "zeroPointers": {
      const p = predictions.zeroPointers;
      if (!p?.driverIds?.length) return <F1Text muted>No prediction yet.</F1Text>;
      return (
        <F1Text>
          {p.driverIds.map((id) => getDriverName(id, drivers)).join(", ")}
        </F1Text>
      );
    }
    case "wildcard": {
      const p = predictions.wildcard;
      if (!p?.statement) return <F1Text muted>No prediction yet.</F1Text>;
      return (
        <span>
          <F1Text className="italic">"{p.statement}"</F1Text>
          {p.pointsPotential != null && (
            <F1Text muted className="ml-2 text-xs">({p.pointsPotential} pts max)</F1Text>
          )}
          {p.fulfilled && <F1Text className="ml-2 text-xs text-f1-gold">✓ Fulfilled</F1Text>}
        </span>
      );
    }
    default:
      return null;
  }
}

type RenderYourPredictionsProps = {
  predictions: UserPredictions | undefined;
  standing: { categoryScores: { categoryId: string; score: number }[] } | undefined;
  onCategoryClick?: (categoryId: PredictionCategoryId) => void;
  /** When false, cards show only title/description/score (no actual prediction data). Use on list view; show data on detail page. */
  showPredictionData?: boolean;
};

export function RenderYourPredictions({ predictions, standing, onCategoryClick, showPredictionData = true }: RenderYourPredictionsProps) {
  const drivers = useDrivers();
  const constructors = useConstructors();
  return (
    <>
      {CATEGORY_IDS.map((categoryId) => (
        <PredictionCategoryCard
          key={categoryId}
          categoryId={categoryId}
          content={showPredictionData ? <PredictionContent predictions={predictions} categoryId={categoryId} drivers={drivers} constructors={constructors} /> : null}
          score={standing ? getCategoryScore(standing, categoryId) : undefined}
          onClick={onCategoryClick ? () => onCategoryClick(categoryId) : undefined}
        />
      ))}
    </>
  );
}

export { PredictionContent };
