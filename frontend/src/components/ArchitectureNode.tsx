import { Handle, Position } from '@xyflow/react';
import type { AstNode, ControllerNode, ServiceNode, EntityNode } from '../types/ast';

/**
 * ArchitectureNode — Custom React Flow node component.
 *
 * Renders a Controller (blue), Service (purple), or Entity (green) node
 * on the visual canvas. Displays annotations, methods, and properties
 * based on node type.
 *
 * The `data` prop contains the full AstNode object.
 */

interface ArchitectureNodeProps {
  data: AstNode;
}

const COLORS = {
  Controller: { bg: '#dbeafe', border: '#3b82f6', header: '#bfdbfe' },
  Service:    { bg: '#f3e8ff', border: '#a855f7', header: '#e9d5ff' },
  Entity:     { bg: '#dcfce3', border: '#22c55e', header: '#bbf7d0' },
} as const;

export default function ArchitectureNode({ data }: ArchitectureNodeProps) {
  const colors = COLORS[data.type];

  return (
    <div style={{
      background: colors.bg,
      border: `2px solid ${colors.border}`,
      borderRadius: '8px',
      minWidth: '200px',
      fontSize: '12px',
      fontFamily: 'sans-serif',
      boxShadow: '0 4px 6px rgba(0,0,0,0.1)',
    }}>
      {/* Socket Handle (Inputs) — top */}
      <Handle type="target" position={Position.Top} style={{ background: '#555' }} />

      {/* Header */}
      <div style={{
        background: colors.header,
        padding: '8px',
        borderTopLeftRadius: '6px',
        borderTopRightRadius: '6px',
        fontWeight: 'bold',
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        borderBottom: `1px solid ${colors.border}`,
      }}>
        <span>{data.name}</span>
        <span style={{ fontSize: '10px', color: '#555', textTransform: 'uppercase' }}>
          {data.type}
        </span>
      </div>

      {/* Body */}
      <div style={{ padding: '8px' }}>
        {/* Annotations */}
        {data.annotations.length > 0 && (
          <div style={{ marginBottom: '8px' }}>
            {data.annotations.map((ann, i) => (
              <span key={i} style={{
                background: 'white',
                padding: '2px 6px',
                borderRadius: '4px',
                fontSize: '10px',
                display: 'inline-block',
                marginRight: '4px',
                border: '1px solid #ccc',
              }}>
                [{ann}]
              </span>
            ))}
          </div>
        )}

        {/* Methods (Controller & Service) */}
        {(data.type === 'Controller' || data.type === 'Service') &&
          renderMethods(data)}

        {/* Properties (Entity) */}
        {data.type === 'Entity' && renderProperties(data)}
      </div>

      {/* Plug Handle (Outputs) — bottom */}
      <Handle type="source" position={Position.Bottom} style={{ background: '#555' }} />
    </div>
  );
}

function renderMethods(data: ControllerNode | ServiceNode) {
  if (data.methods.length === 0) return null;
  return (
    <div>
      <div style={{ fontWeight: 'bold', marginBottom: '4px', borderBottom: '1px solid #ccc' }}>
        Methods
      </div>
      {data.methods.map((m, i) => (
        <div key={i} style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '2px' }}>
          <span>
            {m.verb && <span style={{ color: '#888', marginRight: '4px' }}>{m.verb}</span>}
            {m.name}()
          </span>
          <span style={{ color: '#666' }}>{m.returnType}</span>
        </div>
      ))}
    </div>
  );
}

function renderProperties(data: EntityNode) {
  if (data.properties.length === 0) return null;
  return (
    <div>
      <div style={{ fontWeight: 'bold', marginBottom: '4px', borderBottom: '1px solid #ccc' }}>
        Properties
      </div>
      {data.properties.map((p, i) => (
        <div key={i} style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '2px' }}>
          <span>
            {p.isPrimaryKey && <span style={{ color: '#d97706', marginRight: '4px' }}>🔑</span>}
            {p.name}
          </span>
          <span style={{ color: '#666' }}>{p.type}</span>
        </div>
      ))}
    </div>
  );
}
