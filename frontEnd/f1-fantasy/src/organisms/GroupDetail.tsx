import { BackLink, GroupDetailCard } from "../molecules";
import type { Group } from "../types/group";

type GroupDetailProps = {
  group: Group;
};

export function GroupDetail({ group }: GroupDetailProps) {
  return (
    <div className="space-y-6">
      <BackLink />
      <GroupDetailCard group={group} />
    </div>
  );
}
