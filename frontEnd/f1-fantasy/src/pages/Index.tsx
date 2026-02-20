import { useState, useEffect, useMemo } from "react";
import { useUser } from "@clerk/clerk-react";
import { message } from "antd";
import { useRecoilState, useSetRecoilState, useRecoilValue } from "recoil";
import { AuthTemplate, DashboardTemplate } from "../templates";
import { LandingHeroWithSignIn, DashboardContent, GroupPredictionsView, CategoryDetailView } from "../organisms";
import { LoadingScreen } from "../atoms";
import { CreateGroupModal, JoinGroupModal } from "../molecules";
import { userGroupsState, selectedGroupIdState, allGroupsState, selectedCategoryIdState, groupPredictionsState, driversState, constructorsState, driversFromApiState, constructorsFromApiState } from "../state/atoms";
import { fetchDriversFromApi } from "../api/drivers";
import { fetchConstructorsFromApi } from "../api/constructors";
import type { PredictionCategoryId } from "../types/predictions";
import { MOCK_GROUPS } from "../data/mockGroups";
import { getOrCreateGroupPredictionsData } from "../data/mockPredictions";
import type { Group } from "../types/group";

export default function Index() {
  const { isSignedIn, isLoaded, user } = useUser();
  const currentUserId = user?.id ?? "user-1";
  const currentUserDisplayName =
    ([user?.firstName, user?.lastName].filter(Boolean).join(" ") || user?.username) ?? "You";
  const setUserGroups = useSetRecoilState(userGroupsState);
  const setAllGroups = useSetRecoilState(allGroupsState);
  const setSelectedGroupId = useSetRecoilState(selectedGroupIdState);
  const setSelectedCategoryId = useSetRecoilState(selectedCategoryIdState);
  const setDrivers = useSetRecoilState(driversState);
  const setConstructors = useSetRecoilState(constructorsState);
  const setDriversFromApi = useSetRecoilState(driversFromApiState);
  const setConstructorsFromApi = useSetRecoilState(constructorsFromApiState);
  const selectedGroupId = useRecoilValue(selectedGroupIdState);
  const selectedCategoryId = useRecoilValue(selectedCategoryIdState);
  const userGroups = useRecoilValue(userGroupsState);
  const allGroups = useRecoilValue(allGroupsState);
  const selectedGroup = userGroups.find((g) => g.id === selectedGroupId);
  const [groupData, setGroupData] = useRecoilState(
    groupPredictionsState(selectedGroupId ?? "_none")
  );
  const [createGroupModalOpen, setCreateGroupModalOpen] = useState(false);
  const [joinGroupModalOpen, setJoinGroupModalOpen] = useState(false);
  const [joinInitialCode, setJoinInitialCode] = useState<string | undefined>();

  const mergedDefault = useMemo(() => {
    if (!selectedGroup) return null;
    let p = getOrCreateGroupPredictionsData(
      selectedGroup.id,
      currentUserId,
      currentUserDisplayName
    );
    const firstStanding = p.standings[0];
    const firstPrediction = p.predictions[0];
    if (firstStanding && firstPrediction && firstStanding.userId !== currentUserId) {
      p = {
        ...p,
        standings: p.standings.map((s) =>
          s.userId === firstStanding.userId
            ? { ...s, userId: currentUserId, displayName: currentUserDisplayName }
            : s
        ),
        predictions: p.predictions.map((pred) =>
          pred.userId === firstPrediction.userId
            ? { ...pred, userId: currentUserId, displayName: currentUserDisplayName }
            : pred
        ),
      };
    }
    return { ...p, lockedUserIds: [] };
  }, [selectedGroup?.id, currentUserId, currentUserDisplayName]);

  useEffect(() => {
    let cancelled = false;
    Promise.all([fetchDriversFromApi(), fetchConstructorsFromApi()]).then(([drivers, constructors]) => {
      if (cancelled) return;
      if (drivers != null) {
        setDrivers(drivers);
        setDriversFromApi(true);
      }
      if (constructors != null) {
        setConstructors(constructors);
        setConstructorsFromApi(true);
      }
    });
    return () => { cancelled = true; };
  }, [setDrivers, setConstructors, setDriversFromApi, setConstructorsFromApi]);

  useEffect(() => {
    if (isSignedIn) {
      setUserGroups(MOCK_GROUPS);
      setAllGroups(MOCK_GROUPS);
    }
  }, [isSignedIn, setUserGroups, setAllGroups]);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const code = params.get("join");
    if (code && isSignedIn) {
      setJoinInitialCode(code);
      setJoinGroupModalOpen(true);
      window.history.replaceState({}, "", window.location.pathname);
    }
  }, [isSignedIn]);

  useEffect(() => {
    if (!selectedGroupId) setSelectedCategoryId(null);
  }, [selectedGroupId, setSelectedCategoryId]);

  useEffect(() => {
    if (mergedDefault != null && groupData == null) setGroupData(mergedDefault);
  }, [mergedDefault, groupData, setGroupData]);

  if (!isLoaded) {
    return <LoadingScreen />;
  }

  if (!isSignedIn) {
    return (
      <AuthTemplate>
        <LandingHeroWithSignIn />
      </AuthTemplate>
    );
  }

  if (selectedGroup) {
    const data = groupData ?? mergedDefault ?? { groupId: selectedGroup.id, standings: [], predictions: [], lockedUserIds: [] };

    return (
      <DashboardTemplate>
        {selectedCategoryId ? (
          <CategoryDetailView
            categoryId={selectedCategoryId as PredictionCategoryId}
            data={data}
            setData={setGroupData}
            currentUserId={currentUserId}
          />
        ) : (
          <GroupPredictionsView
            group={selectedGroup}
            data={data}
            setData={setGroupData}
            currentUserId={currentUserId}
            currentUserDisplayName={currentUserDisplayName}
          />
        )}
      </DashboardTemplate>
    );
  }

  const handleGroupCreated = (group: Group) => {
    setUserGroups((prev) => [...prev, group]);
    setAllGroups((prev) => [...prev, group]);
    setCreateGroupModalOpen(false);
    setSelectedGroupId(group.id);
  };

  const findGroupByCode = (code: string) => {
    const normalized = code.trim().toUpperCase();
    return allGroups.find(
      (g) => g.inviteCode?.toUpperCase() === normalized
    );
  };

  const handleJoinGroup = (group: Group) => {
    setUserGroups((prev) =>
      prev.some((g) => g.id === group.id) ? prev : [...prev, group]
    );
    setJoinGroupModalOpen(false);
    setJoinInitialCode(undefined);
    setSelectedGroupId(group.id);
    message.success(`Joined ${group.name}`);
  };

  return (
    <DashboardTemplate>
      <DashboardContent
        groups={userGroups}
        onCreateGroup={() => setCreateGroupModalOpen(true)}
        onJoinGroup={() => {
          setJoinInitialCode(undefined);
          setJoinGroupModalOpen(true);
        }}
      />
      <CreateGroupModal
        open={createGroupModalOpen}
        onClose={() => setCreateGroupModalOpen(false)}
        onCreated={handleGroupCreated}
      />
      <JoinGroupModal
        open={joinGroupModalOpen}
        onClose={() => {
          setJoinGroupModalOpen(false);
          setJoinInitialCode(undefined);
        }}
        onJoined={handleJoinGroup}
        onError={(msg) => message.error(msg)}
        findGroupByCode={findGroupByCode}
        isAlreadyMember={(group) => userGroups.some((g) => g.id === group.id)}
        initialCode={joinInitialCode}
      />
    </DashboardTemplate>
  );
}
