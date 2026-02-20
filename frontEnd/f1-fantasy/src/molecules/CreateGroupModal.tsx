import { Modal, Form, Input } from "antd";
import { F1Button, F1Title } from "../atoms";
import type { Group } from "../types/group";

type CreateGroupModalProps = {
  open: boolean;
  onClose: () => void;
  onCreated: (group: Group) => void;
};

export function CreateGroupModal({ open, onClose, onCreated }: CreateGroupModalProps) {
  const [form] = Form.useForm<{ name: string }>();

  const handleSubmit = () => {
    form.validateFields().then((values) => {
      const newGroup: Group = {
        id: `grp-${Date.now()}`,
        name: values.name.trim(),
        memberCount: 1,
        createdAt: new Date().toISOString(),
        inviteCode: `F1-${Math.random().toString(36).slice(2, 8).toUpperCase()}`,
      };
      onCreated(newGroup);
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
          Create a group
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
          name="name"
          label={<span className="text-f1-silver">Group name</span>}
          rules={[
            { required: true, message: "Enter a group name" },
            { min: 2, message: "At least 2 characters" },
          ]}
        >
          <Input
            placeholder="e.g. Office Legends"
            className="bg-f1-gray border-f1-gray text-f1-silver placeholder:!text-f1-silver/50"
            autoFocus
          />
        </Form.Item>
        <div className="flex justify-end gap-2 pt-2">
          <F1Button onClick={handleCancel}>Cancel</F1Button>
          <F1Button type="primary" htmlType="submit">
            Create group
          </F1Button>
        </div>
      </Form>
    </Modal>
  );
}
