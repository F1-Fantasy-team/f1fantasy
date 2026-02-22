import { LockOutlined, UnlockOutlined, DeleteOutlined, LogoutOutlined, EditOutlined } from "@ant-design/icons";
import { useState } from "react";
import { App, Input, Modal } from "antd";
import { useSetRecoilState, useRecoilValue } from "recoil";
import { BackLink, InviteSection } from "../molecules";
import { firstRaceDateState } from "../state/atoms";
import { StandingsTable } from "../molecules/StandingsTable";
import { RenderYourPredictions } from "../molecules/YourPredictionContent";
import { F1Title, F1Text, F1Button } from "../atoms";
import { selectedCategoryIdState } from "../state/atoms";
import {
  isUserLocked,
  canUserUnlockSelf,
  getEffectiveGroupLocked,
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
  onDeleteGroup?: (groupId: string) => void;
  onLeaveGroup?: (groupId: string) => void;
  onRenameGroup?: (groupId: string, newName: string) => void;
};

export function GroupPredictionsView({
  group,
  data,
  setData,
  currentUserId,
  currentUserDisplayName: _currentUserDisplayName,
  onDeleteGroup,
  onLeaveGroup,
  onRenameGroup,
}: GroupPredictionsViewProps) {
  const { modal } = App.useApp();
  const [renameModalOpen, setRenameModalOpen] = useState(false);
  const [renameValue, setRenameValue] = useState("");
  const setSelectedCategoryId = useSetRecoilState(selectedCategoryIdState);
  const firstRaceDateFromRaces = useRecoilValue(firstRaceDateState);
  const myStanding = data.standings.find((s) => s.userId === currentUserId);
  const myPredictions = data.predictions.find((p) => p.userId === currentUserId);
  const isAdmin = isGroupAdmin(group, currentUserId);
  const isLocked = isUserLocked(group, data, currentUserId, firstRaceDateFromRaces);
  const groupLocked = getEffectiveGroupLocked(group, data, firstRaceDateFromRaces);
  const canUnlockSelf = canUserUnlockSelf(group, data, currentUserId, firstRaceDateFromRaces);
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

  const handleAdminOverrideToggle = () => {
    setData((prev) => ({ ...prev, adminLockOverride: !groupLocked }));
  };

  const handleCategoryClick = (categoryId: PredictionCategoryId) => {
    setSelectedCategoryId(categoryId);
  };

  const handleDeleteGroupClick = () => {
    if (!onDeleteGroup) return;
    modal.confirm({
      title: "Delete group?",
      content: `"${group.name}" will be permanently deleted. All members will lose access. This cannot be undone.`,
      okText: "Delete group",
      okType: "danger",
      cancelText: "Cancel",
      onOk: () => onDeleteGroup(group.id),
    });
  };

  const handleLeaveGroupClick = () => {
    if (!onLeaveGroup) return;
    modal.confirm({
      title: "Leave group?",
      content: `You will be removed from "${group.name}". You can rejoin later with the invite code.`,
      okText: "Leave",
      okType: "danger",
      cancelText: "Cancel",
      onOk: () => onLeaveGroup(group.id),
    });
  };

  const openRenameModal = () => {
    setRenameValue(group.name);
    setRenameModalOpen(true);
  };

  const handleRenameSubmit = () => {
    const trimmed = renameValue.trim();
    if (!trimmed || !onRenameGroup) return;
    onRenameGroup(group.id, trimmed);
    setRenameModalOpen(false);
    setRenameValue("");
  };

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
          {showAdminOverride && groupLocked && (
            <F1Button type="default" size="small" icon={<UnlockOutlined />} onClick={handleAdminOverrideToggle}>
              Unlock predictions
            </F1Button>
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
        <div className="mt-4 flex flex-wrap gap-2 border-t border-f1-gray pt-4">
          {isAdmin && onRenameGroup && (
            <F1Button type="default" size="small" icon={<EditOutlined />} onClick={openRenameModal}>
              Rename group
            </F1Button>
          )}
          {isAdmin && onDeleteGroup && (
            <F1Button type="default" danger size="small" icon={<DeleteOutlined />} onClick={handleDeleteGroupClick}>
              Delete group
            </F1Button>
          )}
          {onLeaveGroup && !isAdmin && (
            <F1Button type="default" size="small" icon={<LogoutOutlined />} onClick={handleLeaveGroupClick}>
              Leave group
            </F1Button>
          )}
        </div>
      </div>

      <Modal
        title="Rename group"
        open={renameModalOpen}
        onOk={handleRenameSubmit}
        onCancel={() => { setRenameModalOpen(false); setRenameValue(""); }}
        okText="Save"
        cancelText="Cancel"
        okButtonProps={{ disabled: !renameValue.trim() }}
        destroyOnHidden
      >
        <Input
          placeholder="Group name"
          value={renameValue}
          onChange={(e) => setRenameValue(e.target.value)}
          onPressEnter={handleRenameSubmit}
          className="mt-3"
          maxLength={100}
          showCount
        />
      </Modal>

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
