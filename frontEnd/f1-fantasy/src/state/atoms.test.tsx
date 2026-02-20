import { describe, it, expect } from "vitest";
import { renderHook, act } from "@testing-library/react";
import { RecoilRoot, useRecoilState, useRecoilValue } from "recoil";
import {
  selectedGroupIdState,
  userGroupsState,
  allGroupsState,
  driversState,
  constructorsState,
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

describe("driversState", () => {
  it("defaults to drivers list (empty when no mock data in repo)", () => {
    const { result } = renderHook(() => useRecoilValue(driversState), {
      wrapper,
    });
    expect(Array.isArray(result.current)).toBe(true);
    result.current.forEach((d) => {
      expect(d).toHaveProperty("id");
      expect(d).toHaveProperty("name");
    });
  });
});

describe("constructorsState", () => {
  it("defaults to constructors list (empty when no mock data in repo)", () => {
    const { result } = renderHook(() => useRecoilValue(constructorsState), {
      wrapper,
    });
    expect(Array.isArray(result.current)).toBe(true);
    result.current.forEach((c) => {
      expect(c).toHaveProperty("id");
      expect(c).toHaveProperty("name");
    });
  });
});
