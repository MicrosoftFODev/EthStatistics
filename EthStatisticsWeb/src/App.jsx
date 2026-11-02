import "./App.css";

function App() {
    const downloadApp = () => {
        // We will replace this with the real installer URL later.
        document.getElementById("download")?.scrollIntoView({
            behavior: "smooth",
        });
    };

    return (
        <div className="site">
            {/* NAVBAR */}
            <header className="navbar">
                <div className="navInner">
                    <a className="logo" href="#home">
                        <div className="logoMark">
                            <span>Ξ</span>
                        </div>

                        <div className="logoText">
                            <strong>EthStatistics</strong>
                            <small>Quantitative Ethereum Analytics</small>
                        </div>
                    </a>

                    <nav className="navLinks">
                        <a href="#features">Features</a>
                        <a href="#technology">Technology</a>
                        <a href="#download">Download</a>
                    </nav>

                    <button className="navDownload" onClick={downloadApp}>
                        Download
                    </button>
                </div>
            </header>

            <main>
                {/* HERO */}
                <section className="hero" id="home">
                    <div className="heroGrid" />
                    <div className="heroGlow heroGlowPurple" />
                    <div className="heroGlow heroGlowBlue" />

                    <div className="heroContent">
                        <div className="eyebrow">
                            <span className="statusDot" />
                            ETHEREUM QUANTITATIVE ANALYTICS
                        </div>

                        <h1>
                            Understand Ethereum
                            <span> through statistics.</span>
                        </h1>

                        <p className="heroText">
                            A desktop analytics platform combining Ethereum market data,
                            statistical analysis, technical indicators and quantitative
                            tools in one clean environment.
                        </p>

                        <div className="heroActions">
                            <button className="buttonPrimary" onClick={downloadApp}>
                                <span className="buttonIcon">↓</span>
                                Download for Windows
                            </button>

                            <a className="buttonSecondary" href="#features">
                                Explore platform
                                <span>→</span>
                            </a>
                        </div>

                        <div className="heroMeta">
                            <div>
                                <span className="check">✓</span>
                                Windows Application
                            </div>

                            <div>
                                <span className="check">✓</span>
                                Statistical Engine
                            </div>

                            <div>
                                <span className="check">✓</span>
                                Ethereum Analytics
                            </div>
                        </div>
                    </div>

                    {/* APP PREVIEW */}
                    <div className="appPreviewWrapper">
                        <div className="previewGlow" />

                        <div className="appPreview">
                            <div className="windowBar">
                                <div className="windowBrand">
                                    <div className="windowLogo">Ξ</div>

                                    <span>EthStatistics</span>
                                </div>

                                <div className="windowActions">
                                    <span>−</span>
                                    <span>□</span>
                                    <span>×</span>
                                </div>
                            </div>

                            <div className="windowContent">
                                {/* SIDEBAR */}
                                <aside className="sidebar">
                                    <div className="sidebarSection">ANALYTICS</div>

                                    <div className="sidebarItem active">
                                        <span className="sidebarIcon">⌂</span>
                                        Dashboard
                                    </div>

                                    <div className="sidebarItem">
                                        <span className="sidebarIcon">⌁</span>
                                        Statistics
                                    </div>

                                    <div className="sidebarItem">
                                        <span className="sidebarIcon">↗</span>
                                        Forecasting
                                    </div>

                                    <div className="sidebarSection second">LEARN</div>

                                    <div className="sidebarItem">
                                        <span className="sidebarIcon">≡</span>
                                        Dictionary
                                    </div>

                                    <div className="sidebarBottom">
                                        <div className="profile">
                                            <div className="avatar">Z</div>

                                            <div>
                                                <strong>EthStatistics</strong>
                                                <small>Analytics Platform</small>
                                            </div>
                                        </div>
                                    </div>
                                </aside>

                                {/* DASHBOARD */}
                                <div className="dashboard">
                                    <div className="dashboardTop">
                                        <div>
                                            <div className="dashboardLabel">MARKET OVERVIEW</div>
                                            <h3>Ethereum Dashboard</h3>
                                            <p>Statistical overview of the selected period</p>
                                        </div>

                                        <div className="periodSelector">
                                            <span className="calendarIcon">□</span>
                                            Last 30 Days
                                            <span className="chevron">⌄</span>
                                        </div>
                                    </div>

                                    {/* STAT CARDS */}
                                    <div className="stats">
                                        <div className="statCard">
                                            <div className="statHeader">
                                                <span>ETH PRICE</span>
                                                <span className="statIcon purple">Ξ</span>
                                            </div>

                                            <strong>$3,842.16</strong>

                                            <div className="statFooter positive">
                                                ↗ 2.48%
                                                <span>current period</span>
                                            </div>
                                        </div>

                                        <div className="statCard">
                                            <div className="statHeader">
                                                <span>MEAN PRICE</span>
                                                <span className="statIcon blue">μ</span>
                                            </div>

                                            <strong>$3,714.82</strong>

                                            <div className="statFooter">
                                                x̄ estimator
                                                <span>30 observations</span>
                                            </div>
                                        </div>

                                        <div className="statCard">
                                            <div className="statHeader">
                                                <span>RSI (14)</span>
                                                <span className="statIcon green">R</span>
                                            </div>

                                            <strong>58.42</strong>

                                            <div className="statFooter greenText">
                                                ● Neutral
                                                <span>momentum</span>
                                            </div>
                                        </div>

                                        <div className="statCard">
                                            <div className="statHeader">
                                                <span>VOLATILITY</span>
                                                <span className="statIcon orange">σ</span>
                                            </div>

                                            <strong>3.84%</strong>

                                            <div className="statFooter">
                                                standard deviation
                                                <span>period</span>
                                            </div>
                                        </div>
                                    </div>

                                    {/* CHART */}
                                    <div className="chartCard">
                                        <div className="chartTop">
                                            <div>
                                                <strong>ETH Price</strong>
                                                <span>Price & Mean</span>
                                            </div>

                                            <div className="chartLegend">
                                                <div>
                                                    <span className="legendDot purpleDot" />
                                                    Price
                                                </div>

                                                <div>
                                                    <span className="legendLine" />
                                                    Mean
                                                </div>
                                            </div>
                                        </div>

                                        <div className="chartContainer">
                                            <div className="priceLabels">
                                                <span>$4,000</span>
                                                <span>$3,800</span>
                                                <span>$3,600</span>
                                                <span>$3,400</span>
                                            </div>

                                            <div className="chartVisual">
                                                <div className="gridLines">
                                                    <span />
                                                    <span />
                                                    <span />
                                                    <span />
                                                </div>

                                                <svg
                                                    className="chartSvg"
                                                    viewBox="0 0 760 230"
                                                    preserveAspectRatio="none"
                                                >
                                                    <defs>
                                                        <linearGradient
                                                            id="priceFill"
                                                            x1="0"
                                                            y1="0"
                                                            x2="0"
                                                            y2="1"
                                                        >
                                                            <stop
                                                                offset="0%"
                                                                stopColor="#8068ff"
                                                                stopOpacity="0.35"
                                                            />

                                                            <stop
                                                                offset="100%"
                                                                stopColor="#8068ff"
                                                                stopOpacity="0"
                                                            />
                                                        </linearGradient>
                                                    </defs>

                                                    <path
                                                        className="chartFill"
                                                        d="
                              M0 185
                              C30 177 42 165 70 168
                              C95 170 105 148 130 153
                              C158 160 170 132 195 138
                              C220 144 235 112 260 119
                              C285 126 305 101 331 106
                              C355 111 370 80 400 91
                              C428 101 442 79 470 72
                              C500 64 516 88 542 67
                              C570 46 584 62 610 47
                              C637 32 654 52 680 36
                              C705 21 729 31 760 18
                              L760 230
                              L0 230
                              Z
                            "
                                                    />

                                                    <path
                                                        className="priceLine"
                                                        d="
                              M0 185
                              C30 177 42 165 70 168
                              C95 170 105 148 130 153
                              C158 160 170 132 195 138
                              C220 144 235 112 260 119
                              C285 126 305 101 331 106
                              C355 111 370 80 400 91
                              C428 101 442 79 470 72
                              C500 64 516 88 542 67
                              C570 46 584 62 610 47
                              C637 32 654 52 680 36
                              C705 21 729 31 760 18
                            "
                                                    />

                                                    <line
                                                        className="meanLine"
                                                        x1="0"
                                                        y1="124"
                                                        x2="760"
                                                        y2="124"
                                                    />

                                                    <circle
                                                        cx="760"
                                                        cy="18"
                                                        r="5"
                                                        className="lastPointOuter"
                                                    />

                                                    <circle
                                                        cx="760"
                                                        cy="18"
                                                        r="2.5"
                                                        className="lastPoint"
                                                    />
                                                </svg>

                                                <div className="chartDates">
                                                    <span>Oct 03</span>
                                                    <span>Oct 10</span>
                                                    <span>Oct 17</span>
                                                    <span>Oct 24</span>
                                                    <span>Nov 01</span>
                                                </div>
                                            </div>
                                        </div>
                                    </div>

                                    {/* RSI */}
                                    <div className="rsiCard">
                                        <div className="rsiHeader">
                                            <div>
                                                <strong>Relative Strength Index</strong>
                                                <span>RSI (14)</span>
                                            </div>

                                            <strong className="rsiValue">58.42</strong>
                                        </div>

                                        <div className="rsiBar">
                                            <div className="rsiLow" />
                                            <div className="rsiNeutral" />
                                            <div className="rsiHigh" />

                                            <span className="rsiMarker" />

                                            <div className="rsiLabels">
                                                <span>Oversold</span>
                                                <span>Neutral</span>
                                                <span>Overbought</span>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </section>

                {/* FEATURES */}
                <section className="featuresSection" id="features">
                    <div className="sectionIntro">
                        <div className="sectionEyebrow">BUILT FOR ANALYSIS</div>

                        <h2>
                            More than a price tracker.
                            <br />
                            <span>A statistical workspace.</span>
                        </h2>

                        <p>
                            EthStatistics combines market information with statistical tools
                            to help you explore Ethereum from a quantitative perspective.
                        </p>
                    </div>

                    <div className="featureGrid">
                        <FeatureCard
                            icon="Σ"
                            number="01"
                            title="Statistical Analysis"
                            text="Explore descriptive statistics and estimators calculated directly from Ethereum market data."
                        />

                        <FeatureCard
                            icon="⌁"
                            number="02"
                            title="Technical Indicators"
                            text="Analyze RSI and other market indicators alongside historical Ethereum price movements."
                        />

                        <FeatureCard
                            icon="↗"
                            number="03"
                            title="Interactive Charts"
                            text="Visualize price, mean estimates and indicators across selectable analysis periods."
                        />

                        <FeatureCard
                            icon="σ"
                            number="04"
                            title="Quantitative Engine"
                            text="A dedicated native calculation layer handles the application's quantitative calculations."
                        />

                        <FeatureCard
                            icon="◎"
                            number="05"
                            title="Forecasting"
                            text="A dedicated workspace for exploring forecasting and quantitative market analysis."
                        />

                        <FeatureCard
                            icon="≡"
                            number="06"
                            title="Statistics Dictionary"
                            text="Review statistical concepts, probability distributions and hypothesis testing in one place."
                        />
                    </div>
                </section>

                {/* TECHNOLOGY */}
                <section className="technologySection" id="technology">
                    <div className="technologyGlow" />

                    <div className="technologyCard">
                        <div className="technologyCopy">
                            <div className="sectionEyebrow">UNDER THE HOOD</div>

                            <h2>Designed as a real quantitative application.</h2>

                            <p>
                                The desktop interface and quantitative calculation layer work
                                together to provide a focused environment for Ethereum
                                statistical analysis.
                            </p>

                            <div className="techTags">
                                <span>C#</span>
                                <span>WPF</span>
                                <span>C++</span>
                                <span>Ethereum</span>
                                <span>Statistics</span>
                            </div>
                        </div>

                        <div className="architecture">
                            <div className="archNode">
                                <div className="archIcon blueArch">C#</div>
                                <div>
                                    <strong>Desktop Application</strong>
                                    <span>Interface & application logic</span>
                                </div>
                            </div>

                            <div className="archConnector">
                                <span />
                                <small>Native Interop</small>
                                <span />
                            </div>

                            <div className="archNode">
                                <div className="archIcon purpleArch">C++</div>

                                <div>
                                    <strong>QuantEngine</strong>
                                    <span>Statistical calculations</span>
                                </div>
                            </div>

                            <div className="archConnector">
                                <span />
                                <small>Data</small>
                                <span />
                            </div>

                            <div className="archNode">
                                <div className="archIcon ethArch">Ξ</div>

                                <div>
                                    <strong>Ethereum Market Data</strong>
                                    <span>Historical observations</span>
                                </div>
                            </div>
                        </div>
                    </div>
                </section>

                {/* DOWNLOAD */}
                <section className="downloadSection" id="download">
                    <div className="downloadGrid" />
                    <div className="downloadGlow" />

                    <div className="downloadContent">
                        <div className="downloadLogo">Ξ</div>

                        <div className="sectionEyebrow">ETHSTATISTICS</div>

                        <h2>
                            Start exploring Ethereum
                            <span> through statistics.</span>
                        </h2>

                        <p>
                            Download the desktop application and explore Ethereum market data
                            through a quantitative lens.
                        </p>

                        <button className="downloadBig">
                            <span className="downloadArrow">↓</span>

                            <span>
                                <strong>Download EthStatistics</strong>
                                <small>Windows Desktop Application</small>
                            </span>
                        </button>

                        <div className="downloadNote">
                            The installer will be connected here once the desktop build is
                            ready.
                        </div>
                    </div>
                </section>
            </main>

            {/* FOOTER */}
            <footer className="footer">
                <div className="footerInner">
                    <div className="footerLogo">
                        <div className="footerMark">Ξ</div>

                        <div>
                            <strong>EthStatistics</strong>
                            <small>Quantitative Ethereum Analytics</small>
                        </div>
                    </div>

                    <div className="footerText">
                        Built for statistical exploration of Ethereum.
                    </div>
                </div>
            </footer>
        </div>
    );
}

function FeatureCard({ icon, number, title, text }) {
    return (
        <article className="featureCard">
            <div className="featureTop">
                <div className="featureIcon">{icon}</div>
                <span className="featureNumber">{number}</span>
            </div>

            <h3>{title}</h3>

            <p>{text}</p>

            <div className="featureBottom">
                <span />
            </div>
        </article>
    );
}

export default App;