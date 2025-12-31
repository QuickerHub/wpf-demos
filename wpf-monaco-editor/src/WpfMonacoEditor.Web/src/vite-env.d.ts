/// <reference types="vite/client" />

// Support for Vite worker imports
declare module '*?worker' {
  const WorkerConstructor: {
    new (): Worker;
  };
  export default WorkerConstructor;
}

