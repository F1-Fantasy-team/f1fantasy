import { describe, it, expect } from "vitest";
import { renderHook, act } from "@testing-library/react";
import { RecoilRoot, useRecoilState, useRecoilValue } from "recoil";
import {
  selectedGroupIdState,
  userGroupsState,
  allGroupsState,
  selectedCategoryIdState,
  driversState,
  constructorsState,
  driversFromApiState,
  constructorsFromApiState,
  firstRaceDateState,
  appDataLoadingState,
} from "./atoms";
import type { Group } from "../types/group";

function wrapper({ children }: { children: React.ReactNode }) {
  return <RecoilRoot>{children}</RecoilRoot>;
}

describe("selectedGroupIdState", () => {
  it("defaults to null", () => {
    const { result } = renderHook(() => useRecoilValue(selectedGroupIdState), {
      wrapper,
    });
    expect(result.current).toBeNull();
  });

  it("can be set", () => {
    const { result } = renderHook(() => useRecoilState(selectedGroupIdState), {
      wrapper,
    });
    act(() => {
      result.current[1]("grp-1");
    });
    expect(result.current[0]).toBe("grp-1");
  });
});

describe("userGroupsState", () => {
  it("defaults to empty array", () => {
    const { result } = renderHook(() => useRecoilValue(userGroupsState), {
      wrapper,
    });
    expect(result.current).toEqual([]);
  });

  it("can be set to a list of groups", () => {
    const groups: Group[] = [
      {
        id: "g1",
        name: "Test",
        memberCount: 1,
        createdAt: "2025-01-01T00:00:00Z",
        adminUserId: "admin-1",
      },
    ];
    const { result } = renderHook(() => useRecoilState(userGroupsState), {
      wrapper,
    });
    act(() => {
      result.current[1](groups);
    });
    expect(result.current[0]).toHaveLength(1);
    expect(result.current[0][0].name).toBe("Test");
  });
});

describe("allGroupsState", () => {
  it("defaults to empty array", () => {
    const { result } = renderHook(() => useRecoilValue(allGroupsState), {
      wrapper,
    });
    expect(result.current).toEqual([]);
  });
});

describe("selectedCategoryIdState", () => {
  it("defaults to null", () => {
    const { result } = renderHook(() => useRecoilValue(selectedCategoryIdState), {
      wrapper,
    });
    expect(result.current).toBeNull();
  });

  it("can be set", () => {
    const { result } = renderHook(() => useRecoilState(selectedCategoryIdState), {
      wrapper,
    });
    act(() => {
      result.current[1]("driversChampionship");
    });
    expect(result.current[0]).toBe("driversChampionship");
  });
});

describe("driversState", () => {
  it("defaults to empty array", () => {
    const { result } = renderHook(() => useRecoilValue(driversState), {
      wrapper,
    });
    expect(result.current).toEqual([]);
  });
});

describe("constructorsState", () => {
  it("defaults to empty array", () => {
    const { result } = renderHook(() => useRecoilValue(constructorsState), {
      wrapper,
    });
    expect(result.current).toEqual([]);
  });
});

describe("driversFromApiState", () => {
  it("defaults to false", () => {
    const { result } = renderHook(() => useRecoilValue(driversFromApiState), {
      wrapper,
    });
    expect(result.current).toBe(false);
  });
});

describe("constructorsFromApiState", () => {
  it("defaults to false", () => {
    const { result } = renderHook(() => useRecoilValue(constructorsFromApiState), {
      wrapper,
    });
    expect(result.current).toBe(false);
  });
});

describe("firstRaceDateState", () => {
  it("defaults to null", () => {
    const { result } = renderHook(() => useRecoilValue(firstRaceDateState), {
      wrapper,
    });
    expect(result.current).toBeNull();
  });

  it("can be set to an ISO date string", () => {
    const { result } = renderHook(() => useRecoilState(firstRaceDateState), {
      wrapper,
    });
    act(() => {
      result.current[1]("2026-03-01");
    });
    expect(result.current[0]).toBe("2026-03-01");
  });
});

describe("appDataLoadingState", () => {
  it("defaults to false", () => {
    const { result } = renderHook(() => useRecoilValue(appDataLoadingState), {
      wrapper,
    });
    expect(result.current).toBe(false);
  });

  it("can be set to true", () => {
    const { result } = renderHook(() => useRecoilState(appDataLoadingState), {
      wrapper,
    });
    act(() => {
      result.current[1](true);
    });
    expect(result.current[0]).toBe(true);
  });
});
