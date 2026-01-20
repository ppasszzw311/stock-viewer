'use client';

import { useEffect, useState } from 'react';

interface Stock {
    code: string;
    name: string;
}

interface DailyAttention {
    id: number;
    stockCode: string;
    date: string;
    reason: string;
    stock: Stock;
}

interface DispositionRecord {
    id: number;
    stockCode: string;
    startDate: string;
    endDate: string;
    measures: string;
    stock: Stock;
}

interface RiskAssessment {
    stockCode: string;
    level: number; // 0: Safe, 1: Warning, 2: Danger
    reason: string;
    consecutiveDays: number;
    daysInLast10: number;
    daysInLast30: number;
}

export default function Home() {
    const [attentionStocks, setAttentionStocks] = useState<DailyAttention[]>([]);
    const [dispositionStocks, setDispositionStocks] = useState<DispositionRecord[]>([]);
    const [riskStocks, setRiskStocks] = useState<RiskAssessment[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchData = async () => {
            try {
                // In a real app, use environment variables for API URL
                // We'll assume a proxy or direct call to localhost:5000 for dev
                const apiBase = 'http://localhost:5000/api/Stock';

                const [attentionRes, dispositionRes, riskRes] = await Promise.all([
                    fetch(`${apiBase}/attention`),
                    fetch(`${apiBase}/disposition`),
                    fetch(`${apiBase}/risk`)
                ]);

                if (attentionRes.ok) setAttentionStocks(await attentionRes.json());
                if (dispositionRes.ok) setDispositionStocks(await dispositionRes.json());
                if (riskRes.ok) setRiskStocks(await riskRes.json());
            } catch (error) {
                console.error('Failed to fetch data', error);
            } finally {
                setLoading(false);
            }
        };

        fetchData();
    }, []);

    if (loading) {
        return <div className="min-h-screen flex items-center justify-center bg-gray-900 text-white">Loading...</div>;
    }

    return (
        <div className="min-h-screen bg-gray-950 text-gray-100 p-8 font-sans">
            <header className="mb-10">
                <h1 className="text-4xl font-bold bg-gradient-to-r from-blue-400 to-purple-500 bg-clip-text text-transparent">
                    Stock Viewer
                </h1>
                <p className="text-gray-400 mt-2">TWSE Attention & Disposition Monitor</p>
            </header>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                {/* Disposition Section (Jail) */}
                <section className="bg-gray-900 rounded-xl p-6 border border-red-900/50 shadow-lg shadow-red-900/20">
                    <h2 className="text-2xl font-semibold text-red-400 mb-4 flex items-center">
                        <span className="mr-2">🔒</span> Jail (處置中)
                    </h2>
                    <div className="space-y-4">
                        {dispositionStocks.length === 0 ? (
                            <p className="text-gray-500 italic">No stocks in disposition.</p>
                        ) : (
                            dispositionStocks.map((record) => (
                                <div key={record.id} className="bg-gray-800 p-4 rounded-lg border border-gray-700">
                                    <div className="flex justify-between items-start">
                                        <div>
                                            <span className="text-lg font-bold text-white">{record.stockCode}</span>
                                            <span className="ml-2 text-gray-300">{record.stock?.name}</span>
                                        </div>
                                        <span className="text-xs bg-red-900 text-red-200 px-2 py-1 rounded">
                                            Until {new Date(record.endDate).toLocaleDateString()}
                                        </span>
                                    </div>
                                    <p className="text-sm text-gray-400 mt-2 line-clamp-2">{record.measures}</p>
                                </div>
                            ))
                        )}
                    </div>
                </section>

                {/* Risk Section (Danger Zone) */}
                <section className="bg-gray-900 rounded-xl p-6 border border-orange-900/50 shadow-lg shadow-orange-900/20">
                    <h2 className="text-2xl font-semibold text-orange-400 mb-4 flex items-center">
                        <span className="mr-2">⚠️</span> Danger Zone (即將處置)
                    </h2>
                    <div className="space-y-4">
                        {riskStocks.length === 0 ? (
                            <p className="text-gray-500 italic">No stocks at high risk.</p>
                        ) : (
                            riskStocks.map((risk) => (
                                <div key={risk.stockCode} className="bg-gray-800 p-4 rounded-lg border border-gray-700">
                                    <div className="flex justify-between items-center">
                                        <span className="text-lg font-bold text-white">{risk.stockCode}</span>
                                        <span className={`text-xs px-2 py-1 rounded ${risk.level === 2 ? 'bg-red-600 text-white' : 'bg-orange-600 text-white'}`}>
                                            {risk.level === 2 ? 'High Risk' : 'Warning'}
                                        </span>
                                    </div>
                                    <div className="mt-3 grid grid-cols-3 gap-2 text-center text-xs">
                                        <div className="bg-gray-700 rounded p-1">
                                            <div className="text-gray-400">Consecutive</div>
                                            <div className="font-bold text-white">{risk.consecutiveDays}</div>
                                        </div>
                                        <div className="bg-gray-700 rounded p-1">
                                            <div className="text-gray-400">Last 10</div>
                                            <div className="font-bold text-white">{risk.daysInLast10}</div>
                                        </div>
                                        <div className="bg-gray-700 rounded p-1">
                                            <div className="text-gray-400">Last 30</div>
                                            <div className="font-bold text-white">{risk.daysInLast30}</div>
                                        </div>
                                    </div>
                                    <p className="text-xs text-gray-500 mt-2">{risk.reason}</p>
                                </div>
                            ))
                        )}
                    </div>
                </section>

                {/* Attention Section (Watch List) */}
                <section className="bg-gray-900 rounded-xl p-6 border border-blue-900/50 shadow-lg shadow-blue-900/20">
                    <h2 className="text-2xl font-semibold text-blue-400 mb-4 flex items-center">
                        <span className="mr-2">👀</span> Watch List (注意股)
                    </h2>
                    <div className="space-y-4 max-h-[600px] overflow-y-auto pr-2 custom-scrollbar">
                        {attentionStocks.length === 0 ? (
                            <p className="text-gray-500 italic">No attention stocks today.</p>
                        ) : (
                            attentionStocks.map((item) => (
                                <div key={item.id} className="bg-gray-800 p-3 rounded-lg border border-gray-700 flex justify-between items-center">
                                    <div>
                                        <span className="font-bold text-white">{item.stockCode}</span>
                                        <span className="ml-2 text-gray-300">{item.stock?.name}</span>
                                    </div>
                                    <div className="text-xs text-gray-500">{new Date(item.date).toLocaleDateString()}</div>
                                </div>
                            ))
                        )}
                    </div>
                </section>
            </div>
        </div>
    );
}
