import { useState, useEffect, useMemo } from "react";
import { useUser } from "@clerk/clerk-react";
import { App } from "antd";
import { useRecoilState, useSetRecoilState, useRecoilValue } from "recoil";
import { AuthTemplate, DashboardTemplate } from "../templates";
import { LandingHeroWithSignIn, DashboardContent, InfoBanner, GroupPredictionsView, CategoryDetailView } from "../organisms";
import { LoadingScreen } from "../atoms";
import { CreateGroupModal, JoinGroupModal } from "../molecules";
import { userGroupsState, selectedGroupIdState, allGroupsState, selectedCategoryIdState, groupPredictionsState, driversState, constructorsState, driversFromApiState, constructorsFromApiState, firstRaceDateState, appDataLoadingState } from "../state/atoms";
import { getApiBaseUrl } from "../api/client";
import { fetchDriversFromApi } from "../api/drivers";
import { fetchConstructorsFromApi } from "../api/constructors";
import { fetchRacesForSeasonFromApi, getFirstRaceDateFromRaces } from "../api/races";
import {
  createGroupFromApi,
  fetchMyGroupsFromApi,
  fetchGroupByInviteCodeFromApi,
  joinGroupFromApi,
  leaveGroupFromApi,
  renameGroupFromApi,
  deleteGroupFromApi,
} from "../api/groups";
import {
  fetchGroupStandingsOnlyFromApi,
  fetchCategoryPredictionFromApi,
  postDriverChampionshipFromApi,
  postConstructorChampionshipFromApi,
  postDriverDraftFromApi,
  postDestructorFromApi,
  postMrSaturdayFromApi,
  postZeroPointerFromApi,
  postWildcardFromApi,
} from "../api/predictions";
import type { PredictionCategoryId } from "../types/predictions";
import { createInitialGroupPredictionsData } from "../utils/groupPredictionsData";
import type { Group } from "../types/group";
import type { PredictionLockMode } from "../types/group";
import type { MemberStanding } from "../types/predictions";

/** Merge API standings with group members so every member appears. Use group.members as fallback when API returns []. */
function mergeStandingsWithGroupMembers(
  apiStandings: MemberStanding[],
  group: Group,
  currentUserId: string,
  currentUserDisplayName: string
): MemberStanding[] {
  const members = group.members?.length
    ? group.members
    : [{ userId: currentUserId, displayName: currentUserDisplayName }];

  const byUserId = new Map(apiStandings.map((s) => [s.userId, s]));

  const merged: MemberStanding[] = members.map(({ userId, displayName }) => {
    const existing = byUserId.get(userId);
    if (existing) return existing;

    const effectiveDisplayName =
      userId === currentUserId
        ? currentUserDisplayName
        : displayName && displayName.trim().length > 0
        ? displayName
        : userId;

    return {
      userId,
      displayName: effectiveDisplayName,
      overallScore: 0,
      rank: 0,
      categoryScores: [],
    };
  });

  merged.sort((a, b) => b.overallScore - a.overallScore);
  return merged.map((s, i) => ({ ...s, rank: i + 1 }));
}

export default function Index() {
  const { message } = App.useApp();
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
  const setFirstRaceDate = useSetRecoilState(firstRaceDateState);
  const setAppDataLoading = useSetRecoilState(appDataLoadingState);
  const selectedGroupId = useRecoilValue(selectedGroupIdState);
  const selectedCategoryId = useRecoilValue(selectedCategoryIdState);
  const userGroups = useRecoilValue(userGroupsState);
  const allGroups = useRecoilValue(allGroupsState);
  const driversFromApi = useRecoilValue(driversFromApiState);
  const constructorsFromApi = useRecoilValue(constructorsFromApiState);
  const firstRaceDate = useRecoilValue(firstRaceDateState);
  const appDataLoading = useRecoilValue(appDataLoadingState);
  const selectedGroup = userGroups.find((g) => g.id === selectedGroupId);
  const [groupData, setGroupData] = useRecoilState(
    groupPredictionsState(selectedGroupId ?? "_none")
  );
  const [createGroupModalOpen, setCreateGroupModalOpen] = useState(false);
  const [joinGroupModalOpen, setJoinGroupModalOpen] = useState(false);
  const [joinInitialCode, setJoinInitialCode] = useState<string | undefined>();

  const mergedDefault = useMemo(() => {
    if (!selectedGroup) return null;
    const p = createInitialGroupPredictionsData(
      selectedGroup.id,
      currentUserId,
      currentUserDisplayName
    );
    return { ...p, lockedUserIds: [] };
  }, [selectedGroup?.id, currentUserId, currentUserDisplayName]);

  // Load drivers, constructors, and first race date only when user selects a group (needed for predictions view).
  // Do not include appDataLoading in deps: setting it to true would re-run the effect, cleanup would set it false, and we'd start another fetch (request loop).
  useEffect(() => {
    const needLoad =
      isSignedIn &&
      selectedGroupId != null &&
      (!driversFromApi || !constructorsFromApi || firstRaceDate === null);
    if (!needLoad || appDataLoading) return;

    let cancelled = false;
    setAppDataLoading(true);
    Promise.all([
      fetchDriversFromApi(),
      fetchConstructorsFromApi(),
      fetchRacesForSeasonFromApi(),
    ]).then(([drivers, constructors, races]) => {
      if (cancelled) return;
      if (drivers != null) {
        setDrivers(drivers);
        setDriversFromApi(true);
      }
      if (constructors != null) {
        setConstructors(constructors);
        setConstructorsFromApi(true);
      }
      if (races != null) {
        const firstDate = getFirstRaceDateFromRaces(races);
        if (firstDate) setFirstRaceDate(firstDate);
      }
    }).finally(() => {
      if (!cancelled) setAppDataLoading(false);
    });
    return () => {
      cancelled = true;
      setAppDataLoading(false);
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps -- appDataLoading intentionally omitted to avoid request loop (see comment above)
  }, [
    isSignedIn,
    selectedGroupId,
    driversFromApi,
    constructorsFromApi,
    firstRaceDate,
    setDrivers,
    setConstructors,
    setDriversFromApi,
    setConstructorsFromApi,
    setFirstRaceDate,
    setAppDataLoading,
  ]);

  useEffect(() => {
    if (!isSignedIn) return;
    if (!getApiBaseUrl()) {
      setUserGroups([]);
      setAllGroups([]);
      return;
    }
    fetchMyGroupsFromApi().then((groups) => {
      if (groups != null) {
        setUserGroups(groups);
        setAllGroups(groups);
      } else {
        setUserGroups([]);
        setAllGroups([]);
      }
    });
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

  // When API is set, fetch only standings for the group (no prediction category GETs → no 404s for new groups).
  useEffect(() => {
    if (
      !selectedGroupId ||
      !selectedGroup ||
      !getApiBaseUrl() ||
      !isSignedIn
    )
      return;
    let cancelled = false;
    fetchGroupStandingsOnlyFromApi(
      selectedGroupId,
      currentUserId,
      currentUserDisplayName
    ).then((result) => {
      if (cancelled || result == null) return;
      const standingsWithAllMembers = mergeStandingsWithGroupMembers(
        result.standings,
        selectedGroup,
        currentUserId,
        currentUserDisplayName
      );
      setGroupData((prev) => ({
        ...(prev ?? {
          groupId: selectedGroupId,
          standings: [],
          predictions: [],
        }),
        standings: standingsWithAllMembers,
        predictions: (prev?.predictions ?? []).filter(
          (p) => p.userId !== currentUserId
        ).concat(result.predictions),
        predictionLock: selectedGroup.predictionsLocked,
      }));
    });
    return () => {
      cancelled = true;
    };
  }, [
    isSignedIn,
    selectedGroupId,
    selectedGroup?.id,
    selectedGroup?.predictionsLocked,
    currentUserId,
    currentUserDisplayName,
    setGroupData,
  ]);

  // When user opens a category, fetch that category's prediction (one GET; 404 only if that category is empty).
  useEffect(() => {
    if (
      !selectedGroupId ||
      !selectedCategoryId ||
      !getApiBaseUrl() ||
      !groupData?.predictions
    )
      return;
    const myPrediction = groupData.predictions.find((p) => p.userId === currentUserId);
    const categoryId = selectedCategoryId as PredictionCategoryId;
    // Skip if we already have a value for this category (including null = loaded but empty)
    if (myPrediction && categoryId in myPrediction) return;
    let cancelled = false;
    fetchCategoryPredictionFromApi(selectedGroupId, categoryId).then((value) => {
      if (cancelled) return;
      setGroupData((prev) => {
        if (!prev?.predictions) return prev;
        return {
          ...prev,
          predictions: prev.predictions.map((p) =>
            p.userId === currentUserId ? { ...p, [categoryId]: value ?? undefined } : p
          ),
        };
      });
    });
    return () => {
      cancelled = true;
    };
  }, [selectedGroupId, selectedCategoryId, groupData?.predictions, currentUserId, setGroupData]);

  const handleDeleteGroup = async (groupId: string) => {
    if (!getApiBaseUrl()) {
      message.error("API not configured");
      return;
    }
    try {
      await deleteGroupFromApi(groupId);
      setUserGroups((prev) => prev.filter((g) => g.id !== groupId));
      setAllGroups((prev) => prev.filter((g) => g.id !== groupId));
      if (selectedGroupId === groupId) {
        setSelectedGroupId(null);
        setSelectedCategoryId(null);
        setGroupData(null);
      }
      message.success("Group deleted");
    } catch (err) {
      message.error(err instanceof Error ? err.message : "Failed to delete group");
    }
  };

  const savePrediction = async (
    groupId: string,
    categoryId: PredictionCategoryId,
    payload: unknown
  ): Promise<void> => {
    if (!getApiBaseUrl()) return;
    try {
      switch (categoryId) {
        case "driversChampionship":
          await postDriverChampionshipFromApi(groupId, payload as { position: number; driverId: string }[]);
          break;
        case "constructorsChampionship":
          await postConstructorChampionshipFromApi(groupId, payload as { position: number; constructorId: string }[]);
          break;
        case "driverDraft":
          await postDriverDraftFromApi(groupId, payload as { driverId1: string; driverId2: string });
          break;
        case "destructors":
          await postDestructorFromApi(groupId, payload as { driverId1: string; driverId2: string });
          break;
        case "mrSaturday":
          await postMrSaturdayFromApi(groupId, payload as { driverId1: string; driverId2: string });
          break;
        case "zeroPointers":
          await postZeroPointerFromApi(groupId, payload as { driverIds: string[] });
          break;
        case "wildcard":
          await postWildcardFromApi(groupId, payload as { statement: string; pointsPotential?: number; fulfilled?: boolean });
          break;
        default:
          return;
      }
      setGroupData((prev) => {
        if (!prev) return prev;
        return {
          ...prev,
          predictions: prev.predictions.map((p) =>
            p.userId === currentUserId
              ? { ...p, [categoryId]: payload }
              : p
          ),
        };
      });
    } catch (err) {
      message.error(err instanceof Error ? err.message : "Failed to save prediction");
      throw err;
    }
  };

  const handleLeaveGroup = async (groupId: string) => {
    if (!getApiBaseUrl()) {
      message.error("API not configured");
      return;
    }
    try {
      await leaveGroupFromApi(groupId);
      setUserGroups((prev) => prev.filter((g) => g.id !== groupId));
      if (selectedGroupId === groupId) {
        setSelectedGroupId(null);
        setSelectedCategoryId(null);
        setGroupData(null);
      }
      message.success("Left group");
    } catch (err) {
      message.error(err instanceof Error ? err.message : "Failed to leave group");
    }
  };

  const handleRenameGroup = async (groupId: string, newName: string) => {
    const trimmed = newName.trim();
    if (!trimmed) return;
    if (!getApiBaseUrl()) {
      message.error("API not configured");
      return;
    }
    try {
      await renameGroupFromApi(groupId, trimmed);
      setUserGroups((prev) =>
        prev.map((g) => (g.id === groupId ? { ...g, name: trimmed } : g))
      );
      setAllGroups((prev) =>
        prev.map((g) => (g.id === groupId ? { ...g, name: trimmed } : g))
      );
      message.success("Group renamed");
    } catch (err) {
      message.error(err instanceof Error ? err.message : "Failed to rename group");
    }
  };

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
            group={selectedGroup}
            categoryId={selectedCategoryId as PredictionCategoryId}
            data={data}
            setData={(newData) => setGroupData(typeof newData === 'function' ? (prev) => newData(prev ?? data) : newData)}
            currentUserId={currentUserId}
            onSavePrediction={
              getApiBaseUrl()
                ? (categoryId, payload) =>
                    savePrediction(selectedGroup.id, categoryId, payload)
                : undefined
            }
          />
        ) : (
          <GroupPredictionsView
            group={selectedGroup}
            data={data}
            setData={(newData) => setGroupData(typeof newData === 'function' ? (prev) => newData(prev ?? data) : newData)}
            currentUserId={currentUserId}
            currentUserDisplayName={currentUserDisplayName}
            onDeleteGroup={handleDeleteGroup}
            onLeaveGroup={handleLeaveGroup}
            onRenameGroup={handleRenameGroup}
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

  const createGroup = async (payload: {
    name: string;
    predictionLockMode: PredictionLockMode;
  }): Promise<Group> => {
    if (!getApiBaseUrl()) throw new Error("API not configured");
    const group = await createGroupFromApi(payload.name, payload.predictionLockMode);
    if (group) return group;
    throw new Error("Failed to create group");
  };

  const findGroupByCode = async (code: string): Promise<Group | undefined> => {
    if (!getApiBaseUrl()) {
      // Fallback to local allGroups when API not configured
      const normalized = code.trim().toUpperCase();
      return allGroups.find((g) => g.inviteCode?.toUpperCase() === normalized);
    }
    const group = await fetchGroupByInviteCodeFromApi(code);
    return group ?? undefined;
  };

  const handleJoinGroup = async (group: Group) => {
    if (getApiBaseUrl()) {
      try {
        await joinGroupFromApi(group.id);
      } catch (err) {
        message.error(err instanceof Error ? err.message : "Failed to join group");
        return;
      }
    }
    setUserGroups((prev) =>
      prev.some((g) => g.id === group.id) ? prev : [...prev, group]
    );
    setJoinGroupModalOpen(false);
    setJoinInitialCode(undefined);
    setSelectedGroupId(group.id);
    message.success(`Joined ${group.name}`);
  };

  return (
    <DashboardTemplate topSection={<InfoBanner />}>
      <DashboardContent
        groups={userGroups}
        onCreateGroup={() => setCreateGroupModalOpen(true)}
        onJoinGroup={() => {
          setJoinInitialCode(undefined);
          setJoinGroupModalOpen(true);
        }}
        showBanner={false}
      />
      <CreateGroupModal
        open={createGroupModalOpen}
        onClose={() => setCreateGroupModalOpen(false)}
        onCreated={handleGroupCreated}
        createGroup={createGroup}
        currentUserId={currentUserId}
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
