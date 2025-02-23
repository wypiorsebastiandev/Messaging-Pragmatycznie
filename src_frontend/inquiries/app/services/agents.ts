const API_URL = 'http://localhost:5112';

export async function getAgent(id: string): Promise<Agent> {
  const response = await fetch(`${API_URL}/agents/${id}`)
  if (!response.ok) {
    throw new Error('Nie udało się pobrać agenta')
  }
  return response.json()
} 