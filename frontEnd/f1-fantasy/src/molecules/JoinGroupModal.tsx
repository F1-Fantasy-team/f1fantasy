import { useEffect } from "react";
import { Modal, Form, Input } from "antd";
import { F1Button, F1Title } from "../atoms";
import type { Group } from "../types/group";

type JoinGroupModalProps = {
  open: boolean;
  onClose: () => void;
  onJoined: (group: Group) => void;
  onError: (message: string) => void;
  /** Look up a group by invite code (mock: search allGroups). */
  findGroupByCode: (code: string) => Group | undefined;
  /** Check if user is already in this group. */
  isAlreadyMember: (group: Group) => boolean;
  /** Pre-fill code (e.g. from ?join= in URL). */
  initialCode?: string;
};

export function JoinGroupModal({
  open,
  onClose,
  onJoined,
  onError,
  findGroupByCode,
  isAlreadyMember,
  initialCode,
}: JoinGroupModalProps) {
  const [form] = Form.useForm<{ code: string }>();

  useEffect(() => {
    if (open && initialCode) {
      form.setFieldValue("code", initialCode);
    }
  }, [open, initialCode, form]);

  const handleSubmit = () => {
    form.validateFields().then((values) => {
      const code = values.code.trim().toUpperCase();
      const group = findGroupByCode(code);
      if (!group) {
        onError("Invalid or expired invite code.");
        return;
      }
      if (isAlreadyMember(group)) {
        onError("You're already in this group.");
        return;
      }
      onJoined(group);
      form.resetFields();
      onClose();
    });
  };

  const handleCancel = () => {
    form.resetFields();
    onClose();
  };

  return (
    <Modal
      open={open}
      onCancel={handleCancel}
      footer={null}
      destroyOnClose
      width={400}
      centered
      className="!max-w-[calc(100vw-2rem)] [&_.ant-modal-content]:border [&_.ant-modal-content]:border-f1-gray [&_.ant-modal-content]:bg-f1-carbon [&_.ant-modal-header]:border-f1-gray [&_.ant-modal-header]:bg-transparent [&_.ant-modal-body]:pt-2"
      styles={{
        content: { backgroundColor: "var(--color-f1-carbon, #1a1a1a)", maxWidth: "min(400px, calc(100vw - 32px))" },
        header: { borderBottomColor: "var(--color-f1-gray, #2d2d2d)" },
      }}
      title={
        <F1Title level={4} className="!mb-0">
          Join a group
        </F1Title>
      }
    >
      <Form
        form={form}
        layout="vertical"
        onFinish={handleSubmit}
        className="mt-4"
      >
        <Form.Item
          name="code"
          label={<span className="text-f1-silver">Invite code</span>}
          rules={[{ required: true, message: "Enter the invite code" }]}
        >
          <Input
            placeholder="e.g. F1-ABC123"
            className="bg-f1-gray border-f1-gray text-f1-silver placeholder:!text-f1-silver/50 uppercase"
            autoFocus
          />
        </Form.Item>
        <div className="flex justify-end gap-2 pt-2">
          <F1Button onClick={handleCancel}>Cancel</F1Button>
          <F1Button type="primary" htmlType="submit">
            Join group
          </F1Button>
        </div>
      </Form>
    </Modal>
  );
}
