import { useEffect, useRef } from "react";
import { useSetRecoilState, useRecoilValue } from "recoil";
import { ArrowLeftOutlined } from "@ant-design/icons";
import { App, InputNumber, Checkbox } from "antd";
import { F1Button, F1Title, F1Text, F1Card } from "../atoms";
import { PredictionContent, getDriverName, getConstructorName } from "../molecules/YourPredictionContent";
import { DriversChampionshipEditor } from "../molecules/DriversChampionshipEditor";
import { ConstructorsChampionshipEditor } from "../molecules/ConstructorsChampionshipEditor";
import { TwoDriverPicker } from "../molecules/TwoDriverPicker";
import { ZeroPointersEditor } from "../molecules/ZeroPointersEditor";
import { WildcardEditor } from "../molecules/WildcardEditor";
import { CATEGORY_LABELS, CATEGORY_DESCRIPTIONS } from "../constants/predictionCategories";
import { selectedCategoryIdState, firstRaceDateState } from "../state/atoms";
import { useDrivers, useConstructors } from "../state/useDriversAndConstructors";
import { isUserLocked, isGroupAdmin } from "../utils/predictionLock";
import { getApiBaseUrl } from "../api/client";
import {
  fetchAllWildcardsFromApi,
  putAdminWildcardPointsFromApi,
  putAdminWildcardFulfilledFromApi,
} from "../api/predictions";
import type { Group } from "../types/group";
import type { PredictionCategoryId } from "../types/predictions";
import type { GroupPredictionsData } from "../types/predictions";
import type { WildcardPrediction } from "../types/predictions";
import type { Driver } from "../types/driver";
import type { Constructor } from "../types/constructor";

function getScore(data: GroupPredictionsData, userId: string, categoryId: PredictionCategoryId): number | undefined {
  return data.standings.find((s) => s.userId === userId)?.categoryScores.find((c) => c.categoryId === categoryId)?.score;
}

/** Use first name only for comparison table headers */
function getFirstName(displayName: string): string {
  const first = displayName.trim().split(/\s+/)[0];
  return first ?? displayName;
}

/** Order so current user is first (column stays sticky after Pos when scrolling) */
function orderPredictionsCurrentUserFirst<T extends { userId: string }>(predictions: T[], currentUserId: string): T[] {
  const current = predictions.find((p) => p.userId === currentUserId);
  const others = predictions.filter((p) => p.userId !== currentUserId);
  return current ? [current, ...others] : predictions;
}

const POS_COL_WIDTH = "2.5rem"; /* match min-w-[2.5rem] */

function EveryoneDriversTable({ data, currentUserId, drivers }: { data: GroupPredictionsData; currentUserId: string; drivers: Driver[] }) {
  const positions = (() => {
    const max = Math.max(0, ...data.predictions.flatMap((p) => (p.driversChampionship ?? []).map((e) => e.position)));
    return Array.from({ length: max }, (_, i) => i + 1);
  })();
  const ordered = orderPredictionsCurrentUserFirst(data.predictions, currentUserId);
  return (
    <div className="min-w-0 overflow-x-auto rounded-lg border border-f1-gray [-webkit-overflow-scrolling:touch]">
      <table className="w-full min-w-[320px] text-sm">
        <thead>
          <tr className="border-b border-f1-gray bg-f1-gray/30 text-left text-f1-silver/90">
            <th className="sticky left-0 z-10 min-w-10 bg-f1-gray/30 py-2 pr-2 font-medium sm:pr-4">Pos</th>
            {ordered.map((u) => {
              const headerName = getFirstName((u.displayName && u.displayName.trim().length > 0) ? u.displayName : u.userId);
              return (
              <th
                key={u.userId}
                className={`min-w-24 whitespace-nowrap py-2 pr-3 font-medium sm:pr-4 ${u.userId === currentUserId ? "sticky z-10 bg-f1-gray/30 shadow-[2px_0_4px_-2px_rgba(0,0,0,0.3)]" : ""}`}
                style={u.userId === currentUserId ? { left: POS_COL_WIDTH } : undefined}
              >
                <span className="block truncate max-w-20 sm:max-w-none">
                  {headerName}
                  {u.userId === currentUserId ? " (you)" : ""}
                </span>
                {getScore(data, u.userId, "driversChampionship") !== undefined && (
                  <span className="ml-1 text-xs font-mono text-f1-gold">
                    {getScore(data, u.userId, "driversChampionship")} pts
                  </span>
                )}
              </th>
            );
            })}
          </tr>
        </thead>
        <tbody>
          {positions.map((pos) => (
            <tr key={pos} className="border-b border-f1-gray/50">
              <td className="sticky left-0 z-10 min-w-10 bg-f1-carbon py-1.5 pr-2 tabular-nums text-f1-silver/80 sm:pr-4">{pos}</td>
              {ordered.map((u) => {
                const entry = (u.driversChampionship ?? []).find((e) => e.position === pos);
                const name = entry ? getDriverName(entry.driverId, drivers) : "—";
                return (
                  <td
                    key={u.userId}
                    className={`min-w-24 py-1.5 pr-3 text-f1-silver sm:pr-4 ${u.userId === currentUserId ? "sticky z-10 bg-f1-carbon font-medium shadow-[2px_0_4px_-2px_rgba(0,0,0,0.3)]" : ""}`}
                    style={u.userId === currentUserId ? { left: POS_COL_WIDTH } : undefined}
                  >
                    {name}
                  </td>
                );
              })}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function EveryoneConstructorsTable({ data, currentUserId, constructors }: { data: GroupPredictionsData; currentUserId: string; constructors: Constructor[] }) {
  const positions = (() => {
    const max = Math.max(0, ...data.predictions.flatMap((p) => (p.constructorsChampionship ?? []).map((e) => e.position)));
    return Array.from({ length: max }, (_, i) => i + 1);
  })();
  const ordered = orderPredictionsCurrentUserFirst(data.predictions, currentUserId);
  return (
    <div className="min-w-0 overflow-x-auto rounded-lg border border-f1-gray [-webkit-overflow-scrolling:touch]">
      <table className="w-full min-w-[320px] text-sm">
        <thead>
          <tr className="border-b border-f1-gray bg-f1-gray/30 text-left text-f1-silver/90">
            <th className="sticky left-0 z-10 min-w-10 bg-f1-gray/30 py-2 pr-2 font-medium sm:pr-4">Pos</th>
            {ordered.map((u) => {
              const headerName = getFirstName((u.displayName && u.displayName.trim().length > 0) ? u.displayName : u.userId);
              return (
              <th
                key={u.userId}
                className={`min-w-24 whitespace-nowrap py-2 pr-3 font-medium sm:pr-4 ${u.userId === currentUserId ? "sticky z-10 bg-f1-gray/30 shadow-[2px_0_4px_-2px_rgba(0,0,0,0.3)]" : ""}`}
                style={u.userId === currentUserId ? { left: POS_COL_WIDTH } : undefined}
              >
                <span className="block max-w-20 truncate sm:max-w-none">
                  {headerName}
                  {u.userId === currentUserId ? " (you)" : ""}
                </span>
                {getScore(data, u.userId, "constructorsChampionship") !== undefined && (
                  <span className="ml-1 text-xs font-mono text-f1-gold">
                    {getScore(data, u.userId, "constructorsChampionship")} pts
                  </span>
                )}
              </th>
            );
            })}
          </tr>
        </thead>
        <tbody>
          {positions.map((pos) => (
            <tr key={pos} className="border-b border-f1-gray/50">
              <td className="sticky left-0 z-10 min-w-10 bg-f1-carbon py-1.5 pr-2 tabular-nums text-f1-silver/80 sm:pr-4">{pos}</td>
              {ordered.map((u) => {
                const entry = (u.constructorsChampionship ?? []).find((e) => e.position === pos);
                const name = entry ? getConstructorName(entry.constructorId, constructors) : "—";
                return (
                  <td
                    key={u.userId}
                    className={`min-w-24 py-1.5 pr-3 text-f1-silver sm:pr-4 ${u.userId === currentUserId ? "sticky z-10 bg-f1-carbon font-medium shadow-[2px_0_4px_-2px_rgba(0,0,0,0.3)]" : ""}`}
                    style={u.userId === currentUserId ? { left: POS_COL_WIDTH } : undefined}
                  >
                    {name}
                  </td>
                );
              })}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

type CategoryDetailViewProps = {
  group: Group;
  categoryId: PredictionCategoryId;
  data: GroupPredictionsData;
  setData: (data: GroupPredictionsData | ((prev: GroupPredictionsData) => GroupPredictionsData)) => void;
  currentUserId: string;
  /** When set (e.g. API), saves to backend then parent updates state. Otherwise only setData. */
  onSavePrediction?: (categoryId: PredictionCategoryId, payload: unknown) => Promise<void>;
};

export function CategoryDetailView({ group, categoryId, data, setData, currentUserId, onSavePrediction }: CategoryDetailViewProps) {
  const { message } = App.useApp();
  const setSelectedCategoryId = useSetRecoilState(selectedCategoryIdState);
  const firstRaceDateFromRaces = useRecoilValue(firstRaceDateState);
  const drivers = useDrivers();
  const constructors = useConstructors();
  const isAdmin = isGroupAdmin(group, currentUserId);

  useEffect(() => {
    if (categoryId !== "wildcard" || !getApiBaseUrl()) {
      return;
    }

    let cancelled = false;

    fetchAllWildcardsFromApi(group.id).then((list) => {
      if (cancelled || !list || !Array.isArray(list) || list.length === 0) return;

      // Merge group wildcards into predictions so "Everyone's predictions"
      // shows wildcard statements for all members that have one.
      setData((prev) => {
        const byUserId = new Map(
          prev.predictions.map((p) => [p.userId, p] as const)
        );

        for (const w of list as Array<{
          userId: string;
          statement: string;
          pointsPotential?: number;
          fulfilled?: boolean;
          fullfilled?: boolean;
        }>) {
          const existing = byUserId.get(w.userId);
          const member = group.members?.find((m) => m.userId === w.userId);
          const displayName =
            existing?.displayName ??
            member?.displayName ??
            w.userId;

          byUserId.set(w.userId, {
            ...(existing ?? { userId: w.userId, displayName }),
            wildcard: {
              statement: w.statement,
              pointsPotential: w.pointsPotential,
              // Backend historically used both "fulfilled" and "fullfilled"; support both.
              fulfilled: w.fulfilled ?? w.fullfilled,
            },
          });
        }

        return {
          ...prev,
          predictions: Array.from(byUserId.values()),
        };
      });
    });

    return () => {
      cancelled = true;
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps -- run once per group/category to avoid hammering /wildcards
  }, [categoryId, group.id]);

  const myPredictions = data.predictions.find((p) => p.userId === currentUserId);
  const isLocked = isUserLocked(group, data, currentUserId, firstRaceDateFromRaces);

  const handleSaveDriversChampionship = async (driversChampionship: { position: number; driverId: string }[]) => {
    if (onSavePrediction) {
      await onSavePrediction("driversChampionship", driversChampionship);
    } else {
      setData((prev) => ({
        ...prev,
        predictions: prev.predictions.map((p) =>
          p.userId === currentUserId ? { ...p, driversChampionship } : p
        ),
      }));
    }
  };

  const handleSaveConstructorsChampionship = async (constructorsChampionship: { position: number; constructorId: string }[]) => {
    if (onSavePrediction) {
      await onSavePrediction("constructorsChampionship", constructorsChampionship);
    } else {
      setData((prev) => ({
        ...prev,
        predictions: prev.predictions.map((p) =>
          p.userId === currentUserId ? { ...p, constructorsChampionship } : p
        ),
      }));
    }
  };

  const handleSaveDriverDraft = async (driverDraft: { driverId1: string; driverId2: string }) => {
    if (onSavePrediction) {
      await onSavePrediction("driverDraft", driverDraft);
    } else {
      setData((prev) => ({
        ...prev,
        predictions: prev.predictions.map((p) =>
          p.userId === currentUserId ? { ...p, driverDraft } : p
        ),
      }));
    }
  };

  const handleSaveDestructors = async (destructors: { driverId1: string; driverId2: string }) => {
    if (onSavePrediction) {
      await onSavePrediction("destructors", destructors);
    } else {
      setData((prev) => ({
        ...prev,
        predictions: prev.predictions.map((p) =>
          p.userId === currentUserId ? { ...p, destructors } : p
        ),
      }));
    }
  };

  const handleSaveMrSaturday = async (mrSaturday: { driverId1: string; driverId2: string }) => {
    if (onSavePrediction) {
      await onSavePrediction("mrSaturday", mrSaturday);
    } else {
      setData((prev) => ({
        ...prev,
        predictions: prev.predictions.map((p) =>
          p.userId === currentUserId ? { ...p, mrSaturday } : p
        ),
      }));
    }
  };

  const handleSaveZeroPointers = async (zeroPointers: { driverIds: string[] }) => {
    if (onSavePrediction) {
      await onSavePrediction("zeroPointers", zeroPointers);
    } else {
      setData((prev) => ({
        ...prev,
        predictions: prev.predictions.map((p) =>
          p.userId === currentUserId ? { ...p, zeroPointers } : p
        ),
      }));
    }
  };

  const handleSaveWildcard = async (wildcard: WildcardPrediction) => {
    if (onSavePrediction) {
      await onSavePrediction("wildcard", wildcard);
    } else {
      setData((prev) => ({
        ...prev,
        predictions: prev.predictions.map((p) =>
          p.userId === currentUserId ? { ...p, wildcard } : p
        ),
      }));
    }
  };

  const handleAdminSetWildcard = async (
    userId: string,
    updates: { pointsPotential?: number; fulfilled?: boolean }
  ) => {
    if (getApiBaseUrl()) {
      try {
        if (updates.pointsPotential != null) {
          const points = Math.max(100, Math.min(200, updates.pointsPotential));
          await putAdminWildcardPointsFromApi(group.id, userId, points);
        }
        if (updates.fulfilled !== undefined) {
          await putAdminWildcardFulfilledFromApi(group.id, userId, updates.fulfilled);
        }
      } catch (err) {
        message.error(err instanceof Error ? err.message : "Failed to update wildcard");
        return;
      }
    }
    setData((prev) => ({
      ...prev,
      predictions: prev.predictions.map((p) => {
        if (p.userId !== userId || !p.wildcard) return p;
        return {
          ...p,
          wildcard: { ...p.wildcard, ...updates },
        };
      }),
    }));
  };

  // Debounce pointsPotential updates so we don't POST on every keypress.
  const pointsDebounceRef = useRef<Record<string, ReturnType<typeof setTimeout>>>({});

  const handleAdminSetWildcardPointsDebounced = (userId: string, rawValue: number | null) => {
    if (rawValue == null) return;
    const clamped = Math.max(100, Math.min(200, rawValue));
    const timers = pointsDebounceRef.current;
    if (timers[userId]) {
      clearTimeout(timers[userId]);
    }
    timers[userId] = setTimeout(() => {
      void handleAdminSetWildcard(userId, { pointsPotential: clamped });
    }, 400);
  };

  return (
    <div className="min-w-0 space-y-6">
      <F1Button
        type="text"
        icon={<ArrowLeftOutlined />}
        onClick={() => setSelectedCategoryId(null)}
        className="min-h-[44px] pl-0"
      >
        Back to predictions
      </F1Button>

      <div className="min-w-0">
        <F1Title level={3} className="mb-1! wrap-break-word! text-xl! sm:text-2xl!">
          {CATEGORY_LABELS[categoryId]}
        </F1Title>
        <F1Text muted className="block">
          {CATEGORY_DESCRIPTIONS[categoryId]}
        </F1Text>
      </div>

      <section>
        {isLocked ? (
          <div className="relative mb-4 overflow-hidden rounded-lg border border-f1-gray bg-f1-carbon/80">
            <div className="flex items-center justify-between gap-3 px-4 py-3">
              <F1Title level={5} className="mb-0!">
                Your prediction
              </F1Title>
              <span className="text-sm text-f1-silver/90">Predictions are locked</span>
            </div>
            <div className="pointer-events-none h-24 overflow-hidden bg-linear-to-b from-f1-carbon to-transparent px-4 pb-4 pt-0">
              <div className="text-xs text-f1-silver/60">
                <PredictionContent predictions={myPredictions} categoryId={categoryId} drivers={drivers} constructors={constructors} />
              </div>
            </div>
          </div>
        ) : (
          <>
            <F1Title level={5} className="mb-2!">
              Your prediction
            </F1Title>
            <F1Card className="mb-4 rounded-lg!">
              {categoryId === "driversChampionship" ? (
                <DriversChampionshipEditor
                  value={myPredictions?.driversChampionship}
                  onSave={handleSaveDriversChampionship}
                />
              ) : categoryId === "constructorsChampionship" ? (
                <ConstructorsChampionshipEditor
                  value={myPredictions?.constructorsChampionship}
                  onSave={handleSaveConstructorsChampionship}
                />
              ) : categoryId === "driverDraft" ? (
                <TwoDriverPicker
                  value={myPredictions?.driverDraft}
                  onSave={handleSaveDriverDraft}
                  labels={["Driver 1", "Driver 2"]}
                />
              ) : categoryId === "destructors" ? (
                <TwoDriverPicker
                  value={myPredictions?.destructors}
                  onSave={handleSaveDestructors}
                  labels={["Driver 1", "Driver 2"]}
                />
              ) : categoryId === "mrSaturday" ? (
                <TwoDriverPicker
                  value={myPredictions?.mrSaturday}
                  onSave={handleSaveMrSaturday}
                  labels={["Driver 1", "Driver 2"]}
                />
              ) : categoryId === "zeroPointers" ? (
                <ZeroPointersEditor
                  value={myPredictions?.zeroPointers}
                  onSave={handleSaveZeroPointers}
                />
              ) : categoryId === "wildcard" ? (
                <WildcardEditor
                  value={myPredictions?.wildcard}
                  onSave={handleSaveWildcard}
                />
              ) : (
                <>
                  <PredictionContent predictions={myPredictions} categoryId={categoryId} drivers={drivers} constructors={constructors} />
                  <p className="mt-2 text-xs text-f1-silver/70">Editing for this category is not available yet.</p>
                </>
              )}
            </F1Card>
          </>
        )}
      </section>

      <section>
        <F1Title level={5} className="mb-2!">
          Everyone&apos;s predictions
        </F1Title>
        {categoryId === "driversChampionship" ? (
          <EveryoneDriversTable
            data={data}
            currentUserId={currentUserId}
            drivers={drivers}
          />
        ) : categoryId === "constructorsChampionship" ? (
          <EveryoneConstructorsTable
            data={data}
            currentUserId={currentUserId}
            constructors={constructors}
          />
        ) : categoryId === "wildcard" ? (
          <div className="flex flex-col gap-4">
            {data.predictions.map((userPrediction) => {
              const w = userPrediction.wildcard;
              const score = data.standings
                .find((s) => s.userId === userPrediction.userId)
                ?.categoryScores.find((c) => c.categoryId === "wildcard")?.score;
              return (
                <F1Card key={userPrediction.userId} className="rounded-lg!">
                  <div className="flex flex-wrap items-start justify-between gap-2">
                    <F1Text className={userPrediction.userId === currentUserId ? "text-f1-red font-medium" : ""}>
                      {getFirstName(
                        (userPrediction.displayName && userPrediction.displayName.trim().length > 0)
                          ? userPrediction.displayName
                          : userPrediction.userId
                      )}
                      {userPrediction.userId === currentUserId ? " (you)" : ""}
                    </F1Text>
                    {score !== undefined && (
                      <span className="text-f1-gold font-mono text-sm">{score} pts</span>
                    )}
                  </div>
                  <div className="mt-2 pt-2 border-t border-f1-gray">
                    {w?.statement ? (
                      <F1Text className="italic">"{w.statement}"</F1Text>
                    ) : (
                      <F1Text muted>No wildcard yet.</F1Text>
                    )}
                    {isAdmin && w?.statement && (
                      <div className="mt-3 flex flex-wrap items-center gap-4">
                        <label className="flex items-center gap-2 text-sm text-f1-silver">
                          <span>Points potential:</span>
                          <InputNumber
                            min={100}
                            max={200}
                            value={w.pointsPotential ?? 100}
                        onChange={(val) => handleAdminSetWildcardPointsDebounced(userPrediction.userId, val)}
                            className="w-20 bg-f1-gray border-f1-gray text-f1-silver [&_.ant-input-number-input]:bg-transparent"
                          />
                        </label>
                        <Checkbox
                          checked={w.fulfilled ?? false}
                          onChange={(e) => handleAdminSetWildcard(userPrediction.userId, { fulfilled: e.target.checked })}
                          className="text-f1-silver [&_.ant-checkbox-inner]:border-f1-gray [&_.ant-checkbox-checked_.ant-checkbox-inner]:bg-f1-red [&_.ant-checkbox-checked_.ant-checkbox-inner]:border-f1-red"
                        >
                          Fulfilled (came true)
                        </Checkbox>
                      </div>
                    )}
                  </div>
                </F1Card>
              );
            })}
          </div>
        ) : (
          <div className="flex flex-col gap-4">
            {data.predictions.map((userPrediction) => (
              <F1Card key={userPrediction.userId} className="rounded-lg!">
                <div className="flex flex-wrap items-start justify-between gap-2">
                  <F1Text className={userPrediction.userId === currentUserId ? "text-f1-red font-medium" : ""}>
                    {getFirstName(
                      (userPrediction.displayName && userPrediction.displayName.trim().length > 0)
                        ? userPrediction.displayName
                        : userPrediction.userId
                    )}
                    {userPrediction.userId === currentUserId ? " (you)" : ""}
                  </F1Text>
                  {data.standings
                    .find((s) => s.userId === userPrediction.userId)
                    ?.categoryScores.find((c) => c.categoryId === categoryId)?.score !== undefined && (
                    <span className="text-f1-gold font-mono text-sm">
                      {data.standings
                        .find((s) => s.userId === userPrediction.userId)
                        ?.categoryScores.find((c) => c.categoryId === categoryId)?.score}{" "}
                      pts
                    </span>
                  )}
                </div>
                <div className="mt-2 pt-2 border-t border-f1-gray">
                  <PredictionContent predictions={userPrediction} categoryId={categoryId} drivers={drivers} constructors={constructors} />
                </div>
              </F1Card>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
