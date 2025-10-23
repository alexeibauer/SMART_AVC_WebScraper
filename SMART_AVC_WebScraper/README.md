# Webscraping of hotel pricing data. Built for SMART recruitment by Alex Valdes Calderon

# Dependencies

- RabbitMQ libraries for .NET
- RabbitMQ server

# Overview

This project is a web scraper designed to extract hotel pricing data from boutique hotel websites. 
It utilizes RabbitMQ for message queuing to handle scraping tasks efficiently.

Projects:

- **WebScraperConsoleApp**: Main web scraping application that processes URLs from user input and sends url as event to RabbitMQ
- **WebScraperWorker**: Worker application that listens to RabbitMQ queue, performs web scraping, and stores the extracted data.
- **WebScraperLogic**: Contains the core logic for web scraping and data extraction.
- **SMARTUtils**: Utility library for common functions used across projects.*

# Examples of URLs to scrape

	1.  https://www.hermosainn.com/ – Hermosa Inn, 43 casitas in Paradise Valley, Arizona. 
	2.	https://www.montecitoinn.com/ – Montecito Inn, historic small hotel in Montecito, California.
	3.	https://www.jeffersondc.com/ – The Jefferson, boutique hotel in Washington D.C. with 99 rooms & suites.
	4.	https://www.21broad.com/ – 21 Broad, a small luxury boutique hotel in Nantucket, Massachusetts.
	5.	https://www.iroquoisnyhotel.com/ – The Iroquois Hotel New York, boutique hotel in Manhattan, New York City.