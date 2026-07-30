import React, { useState, useCallback, useRef } from 'react';
import {
  ReactFlow,
  ReactFlowProvider,
  addEdge,
  useNodesState,
  useEdgesState,
  Controls,
  Background,
} from '@xyflow/react';
import type { Connection, NodeTypes } from '@xyflow/react';
import '@xyflow/react/dist/style.css';

import Sidebar from './components/Sidebar';
import ArchitectureNode from './components/ArchitectureNode';
import type { AstNode, ControllerNode, ServiceNode, EntityNode } from './types/ast';
import type { TemplateAst } from './types/template';

// ---------------------------------------------------------------------------
// Register custom node types with React Flow
// ---------------------------------------------------------------------------

const nodeTypes: NodeTypes = {
  ArchitectureNode: ArchitectureNode as any,
};

let id = 0;
const getId = () => `node_${id++}`;

// ---------------------------------------------------------------------------
// Default node data factories (one per NodeType)
// ---------------------------------------------------------------------------

function makeControllerData(nodeId: string, position: { x: number; y: number }): ControllerNode {
  return {
    id: nodeId,
    type: 'Controller',
    name: 'NewController',
    annotations: ['ApiController'],
    position,
    plugs: [],
    sockets: [],
    routePrefix: 'api/[controller]',
    methods: [
      {
        name: 'Get',
        verb: 'GET',
        route: '',
        annotations: [],
        parameters: [],
        returnType: 'IActionResult',
        body: [],
      },
    ],
  };
}

function makeServiceData(nodeId: string, position: { x: number; y: number }): ServiceNode {
  return {
    id: nodeId,
    type: 'Service',
    name: 'NewService',
    annotations: [],
    position,
    plugs: [],
    sockets: [],
    implements: 'INewService',
    lifetime: 'Scoped',
    methods: [
      {
        name: 'Execute',
        annotations: [],
        parameters: [],
        returnType: 'void',
        body: [],
      },
    ],
  };
}

function makeEntityData(nodeId: string, position: { x: number; y: number }): EntityNode {
  return {
    id: nodeId,
    type: 'Entity',
    name: 'NewEntity',
    annotations: [],
    position,
    plugs: [],
    sockets: [],
    tableName: 'NewEntities',
    properties: [
      { name: 'Id', type: 'int', isPrimaryKey: true, isRequired: true },
      { name: 'Name', type: 'string', isRequired: true, maxLength: 256 },
    ],
  };
}

// ---------------------------------------------------------------------------
// App
// ---------------------------------------------------------------------------

export default function App() {
  const reactFlowWrapper = useRef<HTMLDivElement>(null);
  const [nodes, setNodes, onNodesChange] = useNodesState<any>([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState<any>([]);
  const [reactFlowInstance, setReactFlowInstance] = useState<any>(null);

  const onConnect = useCallback(
    (params: Connection) => setEdges((eds: any[]) => addEdge(params, eds)),
    [setEdges],
  );

  const onDragOver = useCallback((event: React.DragEvent) => {
    event.preventDefault();
    event.dataTransfer.dropEffect = 'move';
  }, []);

  const onDrop = useCallback(
    (event: React.DragEvent) => {
      event.preventDefault();

      const type = event.dataTransfer.getData('application/reactflow');
      if (!type) return;
      if (!reactFlowWrapper.current || !reactFlowInstance) return;

      const reactFlowBounds = reactFlowWrapper.current.getBoundingClientRect();
      const position = reactFlowInstance.screenToFlowPosition({
        x: event.clientX - reactFlowBounds.left,
        y: event.clientY - reactFlowBounds.top,
      });

      const nodeId = getId();

      let data: AstNode;
      switch (type) {
        case 'Controller':
          data = makeControllerData(nodeId, position);
          break;
        case 'Service':
          data = makeServiceData(nodeId, position);
          break;
        case 'Entity':
          data = makeEntityData(nodeId, position);
          break;
        default:
          return;
      }

      setNodes((nds: any[]) => [
        ...nds,
        {
          id: nodeId,
          type: 'ArchitectureNode',
          position,
          data,
        },
      ]);
    },
    [reactFlowInstance, setNodes],
  );

  // -------------------------------------------------------------------------
  // Export the canvas state as AST JSON (PRD §6.1 step 5)
  // -------------------------------------------------------------------------
  const exportAst = () => {
    const ast: TemplateAst = {
      nodes: nodes.map((n: any) => n.data as AstNode),
      edges: edges.map((e: any) => ({
        id: e.id,
        source: e.source,
        sourceHandle: e.sourceHandle ?? undefined,
        target: e.target,
        targetHandle: e.targetHandle ?? undefined,
      })),
    };

    console.log(JSON.stringify(ast, null, 2));
    alert('AST JSON exported to console — open DevTools to see it.');
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100vh', width: '100vw' }}>
      <header
        style={{
          background: '#1e293b',
          color: 'white',
          padding: '12px 24px',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
        }}
      >
        <h1 style={{ margin: 0, fontSize: '20px' }}>ViDev — Visual Developer</h1>
        <button
          onClick={exportAst}
          style={{
            background: '#3b82f6',
            color: 'white',
            border: 'none',
            padding: '8px 16px',
            borderRadius: '4px',
            cursor: 'pointer',
            fontWeight: 'bold',
          }}
        >
          Export AST JSON
        </button>
      </header>

      <div style={{ display: 'flex', flex: 1, overflow: 'hidden' }}>
        <Sidebar />

        <div style={{ flex: 1 }} ref={reactFlowWrapper}>
          <ReactFlowProvider>
            <ReactFlow
              nodes={nodes}
              edges={edges}
              onNodesChange={onNodesChange}
              onEdgesChange={onEdgesChange}
              onConnect={onConnect}
              onInit={setReactFlowInstance}
              onDrop={onDrop}
              onDragOver={onDragOver}
              nodeTypes={nodeTypes}
              fitView
            >
              <Background />
              <Controls />
            </ReactFlow>
          </ReactFlowProvider>
        </div>
      </div>
    </div>
  );
}
