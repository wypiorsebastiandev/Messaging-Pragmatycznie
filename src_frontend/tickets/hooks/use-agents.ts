import { useQuery } from "@tanstack/react-query"

interface Agent {
  id: string
  userId: string
  fullName: string
  position: 'Supervisor' | 'Agent'
  avatarUrl: string
}

async function getAgents(): Promise<Agent[]> {
  const response = await fetch('http://localhost:5112/agents')
  if (!response.ok) {
    throw new Error('Nie udało się pobrać agentów')
  }
  return response.json()
}

export function useAgents() {
  return useQuery({
    queryKey: ['agents'],
    queryFn: getAgents,
  })
}

export function useAgent(id: string) {
  return useQuery({
    queryKey: ['agents', id],
    queryFn: async () => {
      const response = await fetch(`http://localhost:5112/agents/${id}`)
      if (!response.ok) {
        throw new Error('Nie udało się pobrać agenta')
      }
      return response.json()
    },
  })
}

export type { Agent } 