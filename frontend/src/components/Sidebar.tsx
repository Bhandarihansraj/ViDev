import React from 'react';
import { Server, Database, Settings } from 'lucide-react';

export default function Sidebar() {
  const onDragStart = (event: React.DragEvent, nodeType: string) => {
    event.dataTransfer.setData('application/reactflow', nodeType);
    event.dataTransfer.effectAllowed = 'move';
  };

  return (
    <aside style={{
      width: '250px',
      borderRight: '1px solid #ddd',
      padding: '16px',
      background: '#f9fafb',
      display: 'flex',
      flexDirection: 'column',
      gap: '12px'
    }}>
      <h3 style={{ marginBottom: '8px', fontSize: '16px', color: '#333' }}>Architecture Nodes</h3>
      <div style={{ fontSize: '12px', color: '#666', marginBottom: '16px' }}>
        Drag nodes to the canvas to design your architecture.
      </div>

      <div
        style={{
          border: '1px solid #3b82f6',
          borderRadius: '4px',
          padding: '8px 12px',
          background: '#eff6ff',
          cursor: 'grab',
          display: 'flex',
          alignItems: 'center',
          gap: '8px'
        }}
        onDragStart={(event) => onDragStart(event, 'Controller')}
        draggable
      >
        <Server size={18} color="#3b82f6" />
        Controller
      </div>

      <div
        style={{
          border: '1px solid #a855f7',
          borderRadius: '4px',
          padding: '8px 12px',
          background: '#faf5ff',
          cursor: 'grab',
          display: 'flex',
          alignItems: 'center',
          gap: '8px'
        }}
        onDragStart={(event) => onDragStart(event, 'Service')}
        draggable
      >
        <Settings size={18} color="#a855f7" />
        Service
      </div>

      <div
        style={{
          border: '1px solid #22c55e',
          borderRadius: '4px',
          padding: '8px 12px',
          background: '#f0fdf4',
          cursor: 'grab',
          display: 'flex',
          alignItems: 'center',
          gap: '8px'
        }}
        onDragStart={(event) => onDragStart(event, 'Entity')}
        draggable
      >
        <Database size={18} color="#22c55e" />
        Entity
      </div>
    </aside>
  );
}
