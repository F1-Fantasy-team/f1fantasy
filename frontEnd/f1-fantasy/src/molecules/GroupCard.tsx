import { TeamOutlined } from "@ant-design/icons";
import { useSetRecoilState } from "recoil";
import { F1Card, F1Title, F1Text } from "../atoms";
import { selectedGroupIdState } from "../state/atoms";
import type { Group } from "../types/group";

export function GroupCard({ group }: { group: Group }) {
  const setSelectedGroupId = useSetRecoilState(selectedGroupIdState);
  return (
    <F1Card
      hoverable
      onClick={() => setSelectedGroupId(group.id)}
      className="min-w-0"
    >
      <div className="flex min-w-0 items-start justify-between gap-2">
        <div className="min-w-0 flex-1 break-words">
          <F1Title level={5}>{group.name}</F1Title>
          <F1Text muted>{group.memberCount} members</F1Text>
        </div>
        <TeamOutlined className="text-f1-red text-xl" />
      </div>
    </F1Card>
  );
}
