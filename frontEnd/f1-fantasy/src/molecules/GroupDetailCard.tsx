import { F1Card, F1Title, F1Text } from "../atoms";
import { InviteSection } from "./InviteSection";
import type { Group } from "../types/group";

export function GroupDetailCard({ group }: { group: Group }) {
  return (
    <F1Card>
      <F1Title level={3}>{group.name}</F1Title>
      <F1Text muted>{group.memberCount} members</F1Text>
      <InviteSection group={group} />
    </F1Card>
  );
}
