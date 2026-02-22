import { CopyOutlined, LinkOutlined } from "@ant-design/icons";
import { App } from "antd";
import { F1Button, F1Text } from "../atoms";
import type { Group } from "../types/group";

type InviteSectionProps = {
  group: Group;
};

function getInviteLink(inviteCode: string): string {
  const origin = typeof window !== "undefined" ? window.location.origin : "";
  return `${origin}?join=${encodeURIComponent(inviteCode)}`;
}

export function InviteSection({ group }: InviteSectionProps) {
  const { message } = App.useApp();
  if (!group.inviteCode) return null;

  const inviteLink = getInviteLink(group.inviteCode);

  const copyCode = () => {
    void navigator.clipboard.writeText(group.inviteCode!).then(() => {
      message.success("Invite code copied");
    });
  };

  const copyLink = () => {
    void navigator.clipboard.writeText(inviteLink).then(() => {
      message.success("Invite link copied");
    });
  };

  return (
    <div className="mt-4 border-t border-f1-gray pt-4">
      <F1Text muted className="mb-2 block">
        Invite others
      </F1Text>
      <div className="flex min-w-0 flex-wrap items-center gap-2">
        <span className="min-w-0 shrink-0 overflow-hidden text-ellipsis rounded bg-f1-gray px-2 py-1 font-mono text-f1-gold">
          {group.inviteCode}
        </span>
        <div className="flex min-h-[44px] flex-wrap items-center gap-2">
          <F1Button type="primary" size="small" icon={<CopyOutlined />} onClick={copyCode}>
            Copy code
          </F1Button>
          <F1Button size="small" icon={<LinkOutlined />} onClick={copyLink}>
            Copy link
          </F1Button>
        </div>
      </div>
      <F1Text muted className="mt-2 block text-xs">
        Share the code or link so others can join this group.
      </F1Text>
    </div>
  );
}
