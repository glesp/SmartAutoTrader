import axios from 'axios';
import { RecommendationService } from './RecommendationService';
import { Vehicle } from '../models/Vehicle';
import { Config } from '../config/Config';

export class OpenRouterRecommendationService implements RecommendationService {
  private apiKey: string;
  private model: string;

  constructor(config: Config) {
    this.apiKey = config.openRouterApiKey;
    this.model = config.openRouterModel || 'anthropic/claude-3-sonnet:beta';
  }

  async getRecommendation(
    userPreferences: string, 
    budget: number, 
    vehicles: Vehicle[]
  ): Promise<string> {
    if (!this.apiKey) {
      throw new Error('OpenRouter API key not configured');
    }

    const systemPrompt = `You are an automotive expert advisor helping customers find the best vehicle based on their preferences and budget. 
Analyze the available vehicles and the customer's requirements, then recommend the best options.
Your recommendation should include:
1. Top 3 vehicles that match the requirements (if there are that many suitable options)
2. Brief explanation for each recommendation
3. Any specific features that match the customer's preferences
Be concise but thorough, and use a friendly, helpful tone.`;

    const vehicleList = vehicles.map(v => ({
      make: v.make,
      model: v.model,
      year: v.year,
      price: v.price,
      mileage: v.mileage,
      features: v.features,
      condition: v.condition
    }));

    try {
      const response = await axios.post(
        'https://openrouter.ai/api/v1/chat/completions',
        {
          model: this.model,
          messages: [
            {
              role: 'system',
              content: systemPrompt
            },
            {
              role: 'user',
              content: `Given the customer preferences: "${userPreferences}" and a budget of $${budget}, which of these vehicles would you recommend?\n\nAvailable vehicles: ${JSON.stringify(vehicleList, null, 2)}`
            }
          ]
        },
        {
          headers: {
            'Authorization': `Bearer ${this.apiKey}`,
            'Content-Type': 'application/json',
            'HTTP-Referer': 'https://smartautotrader.com'
          }
        }
      );

      return response.data.choices[0].message.content;
    } catch (error) {
      console.error('Error calling OpenRouter API:', error);
      throw new Error('Failed to get recommendation from OpenRouter API');
    }
  }
}
