import React, { useState } from 'react';

interface RecommendationResponse {
  collaborative: number[];
  content: number[];
  azure: number[];
}

const Recommender: React.FC = () => {
  const [contentID, setContentID] = useState<string>('');
  const [recommendations, setRecommendations] =
    useState<RecommendationResponse | null>(null);

  const handleSubmit = async () => {
    const response = await fetch('http://localhost:5164/api/Recommendations', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ contentID }),
    });

    if (response.ok) {
      const data: RecommendationResponse = await response.json();
      setRecommendations(data);
    } else {
      console.error('Failed to fetch recommendations');
    }
  };

  return (
    <div style={{ padding: '1rem' }}>
      <h1>News Recommender</h1>
      <input
        type="text"
        value={contentID}
        onChange={(e) => setContentID(e.target.value)}
        placeholder="Enter User ID"
      />
      <button onClick={handleSubmit}>Get Recommendations</button>

      {recommendations && (
        <div>
          <h2>Collaborative Filtering</h2>
          <ul>
            {recommendations.collaborative.map((id, index) => (
              <li key={`collab-${index}`}>{id}</li>
            ))}
          </ul>

          <h2>Content-Based Filtering</h2>
          <ul>
            {recommendations.content.map((id, index) => (
              <li key={`content-${index}`}>{id}</li>
            ))}
          </ul>

          <h2>Azure ML Recommender</h2>
          <ul>
            {recommendations.azure.map((id, index) => (
              <li key={`azure-${index}`}>{id}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
};

export default Recommender;
