#include "stdafx.h"
#include "Log.h"

#include <log4cpp/Portability.hh>
#include <log4cpp/Category.hh>
#include <log4cpp/PropertyConfigurator.hh>
#include <log4cpp/Appender.hh>
#include <log4cpp/OstreamAppender.hh>
#include <log4cpp/FileAppender.hh>
#include <log4cpp/RollingFileAppender.hh>
#include <log4cpp/PatternLayout.hh>

#pragma comment(lib,"log4cpp")


log4cpp::PatternLayout* NewPatternLayout()
{
	log4cpp::PatternLayout* pLayout = new log4cpp::PatternLayout();
	if(pLayout != NULL)
	{
		pLayout->setConversionPattern("%d: %p %c: %m%n");
	}
	return pLayout;
}

CLog::CLog(const char* logDirectory)
{
	std::string logDirectoryPath = logDirectory;
	if((logDirectoryPath.size() > 0) && (*(logDirectoryPath.end()-1) != '\\'))
		logDirectoryPath += '\\';

	log4cpp::Appender* pSyslogAppender = new log4cpp::OstreamAppender("syslogdummy", &std::cout);
	if(pSyslogAppender != NULL)
	{
		pSyslogAppender->setLayout(::NewPatternLayout());
	}

	size_t maxFileSize = 100*1024;
	log4cpp::Appender* pDecodaAppender = new log4cpp::RollingFileAppender("default", logDirectoryPath+"decoda.log", maxFileSize);
	if(pDecodaAppender != NULL)
	{
		pDecodaAppender->setLayout(::NewPatternLayout());
	}

	log4cpp::Appender* pVsAppender = new log4cpp::RollingFileAppender("default", logDirectoryPath+"vs.log", maxFileSize);
	if(pVsAppender != NULL)
	{
		pVsAppender->setLayout(::NewPatternLayout());
	}

	log4cpp::Category& root = log4cpp::Category::getRoot();
	root.addAppender(pSyslogAppender);
	root.setPriority(log4cpp::Priority::DEBUG);

	log4cpp::Category& decodaLog = log4cpp::Category::getInstance(std::string("DecodaLog"));
	decodaLog.addAppender(pDecodaAppender);

    log4cpp::Category& vsLog = log4cpp::Category::getInstance(std::string("VSLog"));
	vsLog.addAppender(pVsAppender);
}

CLog::~CLog()
{
    log4cpp::Category::shutdown();
}

void CLog::DecodaLog(const char* message)
{
    log4cpp::Category& decodaLog = log4cpp::Category::getInstance(std::string("DecodaLog"));
	decodaLog.debug(message);
}

void CLog::VsLog(const char* message)
{
    log4cpp::Category& vsLog = log4cpp::Category::getInstance(std::string("VSLog"));
	vsLog.debug(message);
}

void CLog::SetDecodaLogMaxFileSize(size_t maxFileSize)
{
	log4cpp::Category& decodaLog = log4cpp::Category::getInstance(std::string("DecodaLog"));
	log4cpp::RollingFileAppender *pDecodaAppender = (log4cpp::RollingFileAppender*)decodaLog.getAppender();
	if(pDecodaAppender != NULL)
	{
		pDecodaAppender->setMaximumFileSize(maxFileSize);
	}
}

void CLog::SetVsLogMaxFileSize(size_t maxFileSize)
{
    log4cpp::Category& vsLog = log4cpp::Category::getInstance(std::string("VSLog"));
	log4cpp::RollingFileAppender *pVsAppender = (log4cpp::RollingFileAppender*)vsLog.getAppender();
	if(pVsAppender != NULL)
	{
		pVsAppender->setMaximumFileSize(maxFileSize);
	}
}
