import { useState } from "react";
import {
    DndContext,
    closestCenter,
    KeyboardSensor,
    PointerSensor,
    useSensor,
    useSensors,
    type DragEndEvent,
} from "@dnd-kit/core";
import {
    arrayMove,
    SortableContext,
    sortableKeyboardCoordinates,
    useSortable,
    verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { HolderOutlined } from "@ant-design/icons";
import { F1Button, F1Text } from "../atoms";
import { useDrivers, useDriversFromApi } from "../state/useDriversAndConstructors";
import type { DriversChampionshipPrediction } from "../types/predictions";

function SortableDriverRow({
    driverId,
    position,
    driverName,
}: {
    driverId: string;
    position: number;
    driverName: string;
}) {
    const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: driverId });

    const style = {
        transform: CSS.Transform.toString(transform),
        transition,
    };

    return (
        <tr
            ref={setNodeRef}
            style={style}
            className={`border-b border-f1-gray/50 ${isDragging ? "opacity-50 bg-f1-gray/50 z-10" : ""}`}>
            <td className="py-1.5 pr-3 tabular-nums text-f1-silver/80 w-10">{position}</td>
            <td className="py-1.5 pr-2">
                <span
                    className="inline-flex cursor-grab touch-none items-center gap-2 active:cursor-grabbing"
                    {...attributes}
                    {...listeners}>
                    <HolderOutlined className="text-f1-silver/60" />
                    <span className="text-f1-silver">{driverName}</span>
                </span>
            </td>
        </tr>
    );
}

type DriversChampionshipEditorProps = {
    value: DriversChampionshipPrediction | undefined;
    onSave: (prediction: DriversChampionshipPrediction) => void;
};

export function DriversChampionshipEditor({ value, onSave }: DriversChampionshipEditorProps) {
    const drivers = useDrivers();
    const fromApi = useDriversFromApi();
    const sorted = (value ?? []).slice().sort((a, b) => a.position - b.position);
    const existingIds = sorted.map((e) => e.driverId);
    const missingIds = drivers.map((d) => d.id).filter((id) => !existingIds.includes(id));
    const [driverOrder, setDriverOrder] = useState<string[]>(() => {
        const n = drivers.length;
        if (existingIds.length === n) return existingIds;
        return [...existingIds, ...missingIds].slice(0, n);
    });
    const getDriverName = (id: string) => drivers.find((d) => d.id === id)?.name ?? id;

    const sensors = useSensors(
        useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
        useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
    );

    const handleDragEnd = (event: DragEndEvent) => {
        const { active, over } = event;
        if (over == null || active.id === over.id) return;
        setDriverOrder((prev) => {
            const oldIndex = prev.indexOf(active.id as string);
            const newIndex = prev.indexOf(over.id as string);
            if (oldIndex === -1 || newIndex === -1) return prev;
            return arrayMove(prev, oldIndex, newIndex);
        });
    };

    const handleSave = () => {
        const prediction: DriversChampionshipPrediction = driverOrder.map((driverId, i) => ({
            position: i + 1,
            driverId,
        }));
        onSave(prediction);
    };

    const driverCount = drivers.length;
    const isValid = driverOrder.length === driverCount && new Set(driverOrder).size === driverCount;

    return (
        <div className="space-y-4">
            <F1Text muted className="block text-xs">
                {`Drag drivers to reorder. Top = P1, bottom = P${driverCount}.`}
            </F1Text>
            <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
                <div className="min-w-0 overflow-x-auto">
                    <table className="w-full min-w-[200px] text-sm">
                        <thead>
                            <tr className="border-b border-f1-gray text-left text-f1-silver/70">
                                <th className="py-1 pr-3 font-normal w-10">Pos</th>
                                <th className="py-1 font-normal">Driver</th>
                            </tr>
                        </thead>
                        <tbody>
                            <SortableContext items={driverOrder} strategy={verticalListSortingStrategy}>
                                {driverOrder.map((driverId, i) => (
                                    <SortableDriverRow
                                        key={driverId}
                                        driverId={driverId}
                                        position={i + 1}
                                        driverName={getDriverName(driverId)}
                                    />
                                ))}
                            </SortableContext>
                        </tbody>
                    </table>
                </div>
            </DndContext>
            <F1Button type="primary" onClick={handleSave} disabled={!isValid}>
                Save prediction
            </F1Button>
            {!isValid && driverOrder.length < driverCount && (
                <F1Text muted className="block text-xs">
                    Add all {driverCount} drivers (use the editor when your list is complete).
                </F1Text>
            )}
        </div>
    );
}
