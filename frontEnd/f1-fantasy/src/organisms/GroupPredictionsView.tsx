import { LockOutlined, UnlockOutlined } from "@ant-design/icons";
import { useSetRecoilState } from "recoil";
import { BackLink, InviteSection } from "../molecules";
import { StandingsTable } from "../molecules/StandingsTable";
import { RenderYourPredictions } from "../molecules/YourPredictionContent";
import { F1Title, F1Text, F1Button } from "../atoms";
import { selectedCategoryIdState } from "../state/atoms";
import {
  isUserLocked,
  canUserUnlockSelf,
  getEffectiveGroupLocked,
  getSystemPredictionsLocked,
  isGroupAdmin,
} from "../utils/predictionLock";
import type { Group } from "../types/group";
import type { GroupPredictionsData } from "../types/predictions";
import type { PredictionCategoryId } from "../types/predictions";

type GroupPredictionsViewProps = {
  group: Group;
  data: GroupPredictionsData;
  setData: (data: GroupPredictionsData | ((prev: GroupPredictionsData) => GroupPredictionsData)) => void;
  currentUserId: string;
  currentUserDisplayName: string;
};

export function GroupPredictionsView({
  group,
  data,
  setData,
  currentUserId,
  currentUserDisplayName: _currentUserDisplayName,
}: GroupPredictionsViewProps) {
  const setSelectedCategoryId = useSetRecoilState(selectedCategoryIdState);
  const myStanding = data.standings.find((s) => s.userId === currentUserId);
  const myPredictions = data.predictions.find((p) => p.userId === currentUserId);
  const isAdmin = isGroupAdmin(group, currentUserId);
  const isLocked = isUserLocked(group, data, currentUserId);
  const groupLocked = getEffectiveGroupLocked(group, data);
  const canUnlockSelf = canUserUnlockSelf(group, data, currentUserId);
  const mode = group.predictionLockMode ?? "hybrid";

  const handleLockSelf = () => {
    setData((prev) => ({
      ...prev,
      lockedUserIds: [...(prev.lockedUserIds ?? []), currentUserId],
    }));
  };

  const handleUnlockSelf = () => {
    setData((prev) => ({
      ...prev,
      lockedUserIds: (prev.lockedUserIds ?? []).filter((id) => id !== currentUserId),
    }));
  };

  const handleAdminLockGroup = () => {
    setData((prev) => ({ ...prev, adminSetPredictionsLocked: true }));
  };

  const handleAdminUnlockGroup = () => {
    setData((prev) => ({ ...prev, adminSetPredictionsLocked: false }));
  };

  const handleAdminOverrideLock = () => {
    setData((prev) => ({ ...prev, adminLockOverride: true }));
  };

  const handleAdminOverrideUnlock = () => {
    setData((prev) => ({ ...prev, adminLockOverride: false }));
  };

  const handleAdminClearOverride = () => {
    setData((prev) => ({ ...prev, adminLockOverride: undefined }));
  };

  const handleCategoryClick = (categoryId: PredictionCategoryId) => {
    setSelectedCategoryId(categoryId);
  };

  const systemLocked = getSystemPredictionsLocked(data);
  const showAdminGroupToggle = isAdmin && mode === "admin";
  const showAdminOverride = isAdmin && mode === "hybrid";

  return (
    <div className="min-w-0 space-y-6">
      <BackLink />
      <div className="min-w-0">
        <F1Title level={3} className="!mb-1 !break-words !text-xl sm:!text-2xl">
          {group.name}
        </F1Title>
        <F1Text muted className="text-sm">{group.memberCount} members</F1Text>
        <div className="mt-2 flex flex-wrap items-center gap-2">
          {showAdminGroupToggle && (
            <>
              {groupLocked ? (
                <F1Button type="default" size="small" icon={<UnlockOutlined />} onClick={handleAdminUnlockGroup}>
                  Unlock predictions for group
                </F1Button>
              ) : (
                <F1Button type="primary" size="small" icon={<LockOutlined />} onClick={handleAdminLockGroup}>
                  Lock predictions for group
                </F1Button>
              )}
            </>
          )}
          {showAdminOverride && (
            <>
              <F1Text muted className="text-sm">
                System: {systemLocked ? "locked" : "unlocked"}
                {data.adminLockOverride !== undefined && ` · Override: ${data.adminLockOverride ? "locked" : "unlocked"}`}
              </F1Text>
              {data.adminLockOverride === undefined ? (
                <>
                  <F1Button type="default" size="small" icon={<LockOutlined />} onClick={handleAdminOverrideLock}>
                    Override: lock
                  </F1Button>
                  <F1Button type="default" size="small" icon={<UnlockOutlined />} onClick={handleAdminOverrideUnlock}>
                    Override: unlock
                  </F1Button>
                </>
              ) : (
                <F1Button type="default" size="small" onClick={handleAdminClearOverride}>
                  Use system default
                </F1Button>
              )}
            </>
          )}
          {!groupLocked && (
            <>
              {isLocked ? (
                <>
                  <F1Text muted className="text-sm">Predictions locked</F1Text>
                  {canUnlockSelf && (
                    <F1Button type="default" size="small" icon={<UnlockOutlined />} onClick={handleUnlockSelf}>
                      Unlock predictions
                    </F1Button>
                  )}
                </>
              ) : (
                <>
                  <F1Text muted className="text-sm">You can edit and lock your predictions below.</F1Text>
                  <F1Button type="primary" size="small" icon={<LockOutlined />} onClick={handleLockSelf}>
                    Lock in predictions
                  </F1Button>
                </>
              )}
            </>
          )}
          {groupLocked && !showAdminGroupToggle && !showAdminOverride && (
            <F1Text muted className="text-sm">Predictions are locked for this group.</F1Text>
          )}
        </div>
        {import.meta.env.DEV && (
          <div className="mt-3 rounded border border-amber-500/50 bg-amber-500/10 px-3 py-2">
            <span className="mr-2 text-xs text-amber-200">Dev only (localhost):</span>
            {isLocked ? (
              canUnlockSelf && (
                <F1Button type="default" size="small" icon={<UnlockOutlined />} onClick={handleUnlockSelf}>
                  Unlock my predictions
                </F1Button>
              )
            ) : (
              !groupLocked && (
                <F1Button type="default" size="small" icon={<LockOutlined />} onClick={handleLockSelf}>
                  Lock my predictions
                </F1Button>
              )
            )}
          </div>
        )}
        <InviteSection group={group} />
      </div>

      <section>
        <F1Title level={5} className="!mb-2">
          Standings
        </F1Title>
        <StandingsTable standings={data.standings} currentUserId={currentUserId} />
      </section>

      <section>
        <F1Title level={5} className="!mb-2">
          Your predictions
        </F1Title>
        <F1Text muted className="block mb-3 text-sm">
          Your picks and scores per category. Click a category to see your full prediction and compare with everyone else.
        </F1Text>
        <div className="grid gap-5 sm:grid-cols-1">
          <RenderYourPredictions
            predictions={myPredictions}
            standing={myStanding}
            onCategoryClick={handleCategoryClick}
            showPredictionData={false}
          />
        </div>
      </section>
    </div>
  );
}
